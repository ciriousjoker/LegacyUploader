using com.ciriousjoker.lib;
using Google.Apis.Upload;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace LegacyManagerUploader
{
    partial class MainWindow
    {
        // File lists
        List<string> IndexedFiles = new List<string>();
        List<Google.Apis.Drive.v2.Data.File> UploadedFiles = new List<Google.Apis.Drive.v2.Data.File>();
        Google.Apis.Drive.v2.Data.File UploadedFile;


        List<UpdatePackage> UpdatePackageList = new List<UpdatePackage>();
        Dictionary<string, HashEntry> HashTable = new Dictionary<string, HashEntry>();

        // Blacklisted extensions
        static List<string> BlacklistedExtensions = new List<string>()
        {
            ".wbfs",
            ".iso"
        };

        public AutoResetEvent stopWaitHandle = new AutoResetEvent(true);
        private string FinalGameName;

        // Main threading
        void workerUpdate(object sender, DoWorkEventArgs e)
        {
            // TODO: Implement more error handling
            BackgroundWorker sendingworker = (BackgroundWorker)sender;


            // Check variables before starting
            if (String.IsNullOrEmpty(ChosenTempFolder) || String.IsNullOrEmpty(ChosenProjectFolder))
            {
                Application.Current.Dispatcher.Invoke(new Action(() => {
                    new MaterialDialog("", "Please choose both folders.").ShowDialog();
                }));
                e.Cancel = true;
                return;
            }

            // Set up / Clean up variables
            UpdatePackageList = new List<UpdatePackage>();
            HashTable = new Dictionary<string, HashEntry>();
            mDriveDirectory = createRootFolder();

            string GameFile = Path.Combine(ChosenTempFolder, "gamefile.7z");
            string GameFileEncrypted = Path.Combine(ChosenTempFolder, "gamefile.7z.enc");

            // TODO: Ask for information
            // - Versionstring, Description, PatchFor etc.
            // - Set Versionnumber automatically

            
            askFinalGameName();

            string GameFileFinal = Path.Combine(ChosenTempFolder, FinalGameName);

            
            sendingworker.ReportProgress(5, new StatusHolder() { txt = "# Indexing all project files, this might take a while. Please be patient." + Environment.NewLine, progress = -1 });
            
            _7zip.CreateZip(ChosenProjectFolder, GameFile, sendingworker);

            if (sendingworker.CancellationPending) { e.Cancel = true; return; }
            sendingworker.ReportProgress(30, new StatusHolder() { txt = "--> Finished compressing." + Environment.NewLine, progress = -1 });

            // Encrypt all the packages
            sendingworker.ReportProgress(25, new StatusHolder() { txt = "# Encrypting the package." + Environment.NewLine, progress = -1 });

            CryptLib.AES_Encrypt(GameFile, GameFileEncrypted, EncryptionPassword, sendingworker);
            File.Delete(GameFile);
            File.Delete(GameFileFinal);
            File.Move(GameFileEncrypted, GameFileFinal);

            if (sendingworker.CancellationPending) { e.Cancel = true; return; }
            sendingworker.ReportProgress(30, new StatusHolder() { txt = "--> Finished encrypting." + Environment.NewLine, progress = -1 });

            // Show notification for the point of no return
            showContinueNotification();
            if (sendingworker.CancellationPending) { e.Cancel = true; return; }

            // Uploading the packages to Google Drive
            sendingworker.ReportProgress(35, new StatusHolder() { txt = "# Uploading packages to Google Drive", progress = -1 });
            uploadGame(GameFileFinal, sendingworker);

            if (sendingworker.CancellationPending) { e.Cancel = true; return; }
            sendingworker.ReportProgress(40, new StatusHolder() { txt = "--> Finished uploading the packages." + Environment.NewLine, progress = -1 });


            sendingworker.ReportProgress(45, new StatusHolder() { txt = "# Package uploaded, here's the link:", progress = -1 });

            // Print out the download url
            string DownloadUrl = "https://drive.google.com/uc?export=download&id=" + UploadedFile.Id;
            sendingworker.ReportProgress(50, new StatusHolder() { txt = DownloadUrl, progress = -1 });
            showCopyableText("Here's the download url:", DownloadUrl);

            // Print out the size in bytes
            sendingworker.ReportProgress(55, new StatusHolder() { txt = "Size in bytes: ", progress = -1 });
            long Size = new FileInfo(GameFileFinal).Length;
            sendingworker.ReportProgress(60, new StatusHolder() { txt = Size.ToString(), progress = -1 });
            showCopyableText("Size in bytes:", Size.ToString());


        }

        // Functionality
        private Google.Apis.Drive.v2.Data.File createRootFolder()
        {
            Google.Apis.Drive.v2.Data.File dir;
            string folder_id = Ini.read(Ini.KEY_DRIVE_ROOT);

            if (String.IsNullOrEmpty(folder_id))
            {
                dir = GoogleDriveApi.createDirectory(mDriveService, AppName, AppDescription);
                Ini.write(Ini.KEY_DRIVE_ROOT, dir.Id);
            }
            else
            {
                dir = GoogleDriveApi.createDirectory(mDriveService, AppName, AppDescription, folder_id);
            }
            return dir;
        }

        public List<string> DirSearch(string sDir, ref long FileCounter, BackgroundWorker sendingworker)
        {
            List<string> files = new List<string>();
            if (sendingworker.CancellationPending) { return files; }
            try
            {
                var FileList = Directory.GetFiles(sDir);
                foreach (string f in FileList)
                {
                    files.Add(f);
                }
                FileCounter += FileList.Length;
                if (sendingworker.CancellationPending) { return files; }
                sendingworker.ReportProgress(50, new StatusHolder() { txt = "Found " + FileCounter + " files.", progress = -2 });
                foreach (string d in Directory.GetDirectories(sDir))
                {
                    files.AddRange(DirSearch(d, ref FileCounter, sendingworker));
                }
            }
            catch (Exception e)
            {
                Application.Current.Dispatcher.Invoke(new Action(() => {
                    ShowErrorReport(e);
                }));
            }

            return files;
        }

        public static string GenerateHash(string filename)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filename))
                {
                    return Convert.ToBase64String(md5.ComputeHash(stream));
                }
            }
        }
        
        private void askFinalGameName()
        {
            // Ask for the filename before starting the progress
            Application.Current.Dispatcher.Invoke(new Action(() => {
                MaterialDialog dialog = new MaterialDialog("Choose a filename");
                dialog.setInput();
                dialog.setButtonPositive();
                dialog.setButtonNeutral();

                MaterialDialogResult result = dialog.ShowReturnResult();

                if (String.IsNullOrWhiteSpace(result.TextInput) || result.ButtonId != MaterialDialog.BUTTON_ID.POSITIVE)
                {
                    new MaterialDialog("Error", "You have to set a password in order to continue.").ShowDialog();
                    setButtonsToStandard();
                    workerPublishUpdate.CancelAsync();
                    return;
                }
                FinalGameName = result.TextInput;
            }));
        }

        private void showContinueNotification()
        {
            // This is used to indicate, that the user should use his brain before publishing any update that might affect hundreds of PCs within a day
            Application.Current.Dispatcher.Invoke(new Action(() => {
                MaterialDialog dialog = new MaterialDialog("Are you sure?", "From now on, you cannot safely cancel or undo the process. Continue?");
                dialog.setButtonPositive();
                dialog.setButtonNeutral();

                var result = dialog.ShowReturnResult();
                
                if(result.ButtonId != MaterialDialog.BUTTON_ID.POSITIVE)
                {
                    workerPublishUpdate.CancelAsync();
                }
            }));
        }

        private void showCopyableText(string headline, string url)
        {
            // This shows the DownloadUrl that has to be pasted into the db.json
            Application.Current.Dispatcher.Invoke(new Action(() => {
                MaterialDialog dialog = new MaterialDialog(headline);
                dialog.setButtonPositive();
                dialog.setInput(url);

                dialog.ShowReturnResult();
            }));
        }

        private void uploadGame(string GameFileFinal, BackgroundWorker sendingworker)
        {
            // Add new line so that the previous message isn't overwritten
            sendingworker.ReportProgress(0, new StatusHolder() { txt = "", progress = -1 });

            stopWaitHandle.Reset();
            GoogleDriveApi.UploadGame(mDriveService, GameFileFinal, mDriveDirectory, onFileUploadProgressChanged, onFileUploaded, sendingworker);
            stopWaitHandle.WaitOne();
        }

        // Event Listeners
        void onFileUploadProgressChanged(IUploadProgress progress, BackgroundWorker sendingworker, string filename, long filesize)
        {
            if (sendingworker.CancellationPending) { stopWaitHandle.Set(); GoogleDriveApi.stopWaitHandle.Set(); return; }

            int percent = (int)Math.Round((double)100 * progress.BytesSent / filesize);
            if (progress.BytesSent != filesize)
            {
                sendingworker.ReportProgress(0, new StatusHolder() { txt = "Uploading part " + filename + " of " + UpdatePackageList.Count + ": ", progress = percent });
            }
        }

        void onFileUploaded(Google.Apis.Drive.v2.Data.File file, BackgroundWorker sendingworker)
        {
            //UploadedFiles.Add(file);
            UploadedFile = file;
            sendingworker.ReportProgress(0, new StatusHolder() { txt = "Uploading part " + Path.GetFileNameWithoutExtension(file.Title) + " of " + UpdatePackageList.Count + ": ", progress = 100 });
            GoogleDriveApi.stopWaitHandle.Set();
            stopWaitHandle.Set();
        }

        private void workerProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e.UserState != null)
            {
                StatusHolder status = (StatusHolder)e.UserState;
                if (status.progress >= 0)
                {
                    status.txt = status.txt + status.progress + " %";
                    SetStatus(status.txt, true);
                }
                else if(status.progress == -1)
                {
                    SetStatus(status.txt);
                }
                else if(status.progress == -2)
                {
                    SetStatus(status.txt, true);
                }
            }
        }

        private void workerUpdateCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            BackgroundWorker sendingworker = (BackgroundWorker)sender;

            if (!e.Cancelled && e.Error == null)
            {
                txt_output.Text += Environment.NewLine + Environment.NewLine + "Finished successfully." + Environment.NewLine;
            }
            else if (e.Cancelled)
            {
                txt_output.Text += Environment.NewLine + Environment.NewLine + "Cancelled." + Environment.NewLine;
            }
            else
            {
                ShowErrorReport(e.Error);
            }

            setButtonsToStandard();
            pb_upload.Visibility = Visibility.Hidden;
        }

        private void ShowErrorReport(Exception e)
        {
            string msg = "1) Do not freak out about this error" + Environment.NewLine;
            msg += "2) Be kind and report this error. You can contact me via Github (ciriousjoker) or via email (ciriousjoker@gmail.com)." + Environment.NewLine;
            msg += "Try to include the " + AppLogFile + " file, it's located in the same directory as the executable." + Environment.NewLine;
            msg += Environment.NewLine + Environment.NewLine + "3) Here's the complete error if you want to do the bugfixing yourself:" + Environment.NewLine;
            msg += e.Message;

            new MaterialDialog("Task failed", msg).ShowDialog();
        }

        // Helper classes
        public class StatusHolder
        {
            public string txt;
            public int progress;
        }
    }
}
