using Google.Apis.Drive.v2;
using SharpConfig;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Diagnostics;
using System.ComponentModel;
using System.Text.RegularExpressions;
using MahApps.Metro;
using MaterialDesignThemes.Wpf;
using System.Windows.Media.Animation;
using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json.Linq;

namespace LegacyManagerUploader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        // Constants
        public static string AppName = "LegacyManagerUploader";
        public static string AppDescription = "This is the folder used to provide updates for Smash Bros Legacy.";
        public static string AppLogFile = "debug.log";
        
        // Google Drive Api
        DriveService mDriveService;
        Google.Apis.Drive.v2.Data.File mDriveDirectory;


        // Settings
        private string ChosenProjectFolder;
        private string ChosenTempFolder;


        // Threading stuff
        private BackgroundWorker workerPublishUpdate;
        private string EncryptionPassword;

        // Routed events
        public static readonly RoutedEvent CloseEvent = EventManager.RegisterRoutedEvent(
        "DoClose", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(MainWindow));
        public event RoutedEventHandler DoClose
        {
            add { AddHandler(CloseEvent, value); }
            remove { RemoveHandler(CloseEvent, value); }
        }

        public MainWindow()
        {
            InitializeComponent();

            Ini.create();
            loadSettings();
            checkCredentials();

            checkTools();

            //TODO: Show instructions on first launch
        }

        private void checkTools()
        {
            if( Directory.Exists("tools") &&
                File.Exists(Path.Combine("tools", "7z.dll")) &&
                File.Exists(Path.Combine("tools", "7z64.dll")))
            {
                return;
            }
            //MessageBox.Show("Some files not found");
            new MaterialDialog("Error", "Some tools are missing. Please reinstall this program.").ShowDialog();
            Application.Current.Shutdown();
        }

        private void checkCredentials()
        {
            if (!Directory.Exists(GoogleDriveApi.CREDENTIAL_STORE))
            {
                setButtonsToAuthenticationRequired();
            } else {
                loadCredentials(false);
            }
        }
        
        private void loadCredentials(bool user_initiated)
        {
            setButtonsToAuthenticationRequired();
            btn_authenticate.IsEnabled = false;
            btn_authenticate.Content = "Connecting ...";

            BackgroundWorker workerConnectToGoogleDrive = new BackgroundWorker();

            workerConnectToGoogleDrive.DoWork += delegate
            {
                mDriveService = GoogleDriveApi.CreateService();
            };
            

            workerConnectToGoogleDrive.RunWorkerCompleted += delegate
            {
                if (mDriveService == null)
                {
                    if (user_initiated)
                    {
                        new MaterialDialog("Authentication required", "A connection to your Google Drive is required. \nPlease try again.").ShowDialog();
                        setButtonsToAuthenticationRequired();
                    }
                    else
                    {
                        // Clear local credentials and ask the user again for them
                        if (Directory.Exists(GoogleDriveApi.CREDENTIAL_STORE))
                        {
                            Directory.Delete(GoogleDriveApi.CREDENTIAL_STORE, true);
                        }
                        new MaterialDialog("Authentication required", "Couldn't connect to your Google Drive.\n Try restarting the app.").ShowDialog();
                        RaiseCloseEvent();
                    }
                } else
                {
                    setButtonsToStandard();
                }
            };
            workerConnectToGoogleDrive.RunWorkerAsync();
        }


        // Functionality
        private void StartProgress()
        {
            setButtonsToUploading();
            pb_upload.Visibility = Visibility.Visible;
            txt_output.Text = "";


            // Check for password
            EncryptionPassword = Ini.read(Ini.KEY_ENCRYPTION_PASSWORD);

            if (String.IsNullOrWhiteSpace(EncryptionPassword))
            {
                MaterialDialog dialogAskPassword = new MaterialDialog("Set a password");
                dialogAskPassword.setInput();
                dialogAskPassword.setButtonPositive();
                dialogAskPassword.setButtonNeutral();

                MaterialDialogResult result = dialogAskPassword.ShowReturnResult();

                if(String.IsNullOrWhiteSpace(result.TextInput) || result.ButtonId != MaterialDialog.BUTTON_ID.POSITIVE)
                {
                    new MaterialDialog("Error", "You have to set a password in order to continue.").ShowDialog();
                    setButtonsToStandard();
                    return;
                }
                EncryptionPassword = result.TextInput;
                Ini.write(Ini.KEY_ENCRYPTION_PASSWORD, EncryptionPassword);
            }
            
            if (workerPublishUpdate != null)
            {
                if (workerPublishUpdate.IsBusy)
                {
                    new MaterialDialog("Error", "Please wait for the current operation to finish ...").ShowDialog();
                    return;
                }
            }

            workerPublishUpdate = new BackgroundWorker();

            workerPublishUpdate.DoWork += new DoWorkEventHandler((senderRepair, eRepair) => workerUpdate(senderRepair, eRepair));

            workerPublishUpdate.ProgressChanged += new ProgressChangedEventHandler(workerProgressChanged);
            workerPublishUpdate.RunWorkerCompleted += new RunWorkerCompletedEventHandler(workerUpdateCompleted);
            workerPublishUpdate.WorkerReportsProgress = true;
            workerPublishUpdate.WorkerSupportsCancellation = true;

            workerPublishUpdate.RunWorkerAsync();
        }

        private void StopProgress()
        {
            if (workerPublishUpdate != null && !workerPublishUpdate.CancellationPending)
            {
                workerPublishUpdate.CancelAsync();
                SetStatus(Environment.NewLine + Environment.NewLine + "Cancelling, please wait...");
            }
        }


        // Helper functions
        private void loadSettings()
        {
            tb_chosen_folder.Text = Ini.read(Ini.KEY_LOCAL_FOLDER);
            ChosenProjectFolder = Ini.read(Ini.KEY_LOCAL_FOLDER);

            tb_chosen_temp_folder.Text = Ini.read(Ini.KEY_LOCAL_TEMP_FOLDER);
            ChosenTempFolder = Ini.read(Ini.KEY_LOCAL_TEMP_FOLDER);
        }

        // Different button states depending on what's needed
        private void setButtonsToUploading()
        {
            setButtonsToStandard();

            btn_choose_folder.IsEnabled = false;
            btn_choose_tmp_folder.IsEnabled = false;
            btn_upload.IsEnabled = false;
            btn_cancel.IsEnabled = true;
            btn_authenticate.Visibility = Visibility.Hidden;
            tv_project_folder.IsEnabled = true;
            tv_temp_folder.IsEnabled = true;
        }
        private void setButtonsToAuthenticationRequired()
        {
            btn_choose_folder.IsEnabled = false;
            btn_choose_tmp_folder.IsEnabled = false;
            btn_upload.IsEnabled = false;
            btn_cancel.IsEnabled = false;
            btn_authenticate.Visibility = Visibility.Visible;
            btn_authenticate.Content = "Connect to Google Drive";
            btn_authenticate.IsEnabled = true;
            tv_project_folder.IsEnabled = false;
            tv_temp_folder.IsEnabled = false;
        }
        private void setButtonsToStandard()
        {
            if (mDriveService == null)
            {
                setButtonsToAuthenticationRequired();
            }

            btn_choose_folder.IsEnabled = true;
            btn_choose_tmp_folder.IsEnabled = true;
            btn_cancel.IsEnabled = false;

            if(Directory.Exists(ChosenProjectFolder) && Directory.Exists(ChosenTempFolder))
            {
                btn_upload.IsEnabled = true;
            }

            btn_authenticate.Visibility = Visibility.Hidden;
            tv_project_folder.IsEnabled = true;
            tv_temp_folder.IsEnabled = true;
        }

        void SetStatus(string Text, bool ReplaceLastLine = false)
        {
            if(ReplaceLastLine)
            {
                // Remove the last line but preserve the line break
                string output = txt_output.Text; // Possibly unneccessary
                txt_output.Text = output.Remove(output.LastIndexOf(Environment.NewLine));
                //txt_output.Text += Environment.NewLine;
            }

            // Replace it with an updated line
            //txt_output.Text += Environment.NewLine + Text + progress + " %";
            txt_output.Text += Environment.NewLine + Text;

            sv_output.ScrollToEnd();
            File.AppendAllText(AppLogFile, "\t" + Text + "\n");
        }

        /*
        void SetStatus(string txt)
        {
            // Add line to the output textblock
            txt_output.Text += Environment.NewLine + txt;

            sv_output.ScrollToEnd();
            File.AppendAllText(AppLogFile, Environment.NewLine + txt);
        }
        */

        // UI Functionality
        private void btn_choose_folder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog();
            dialog.AddToMostRecentlyUsedList = false;
            dialog.IsFolderPicker = true;
            dialog.Multiselect = false;
            dialog.Title = "Select a temporary folder";

            if (!String.IsNullOrWhiteSpace(ChosenProjectFolder) && Directory.Exists(ChosenProjectFolder))
            {
                dialog.InitialDirectory = ChosenProjectFolder;
            }

            CommonFileDialogResult result = dialog.ShowDialog();

            if (result == CommonFileDialogResult.Ok && Directory.Exists(dialog.FileName))
            {
                if (ValidateFolders(dialog.FileName, ChosenTempFolder))
                {
                    tb_chosen_folder.Text = dialog.FileName;
                    ChosenProjectFolder = dialog.FileName;
                    Ini.write(Ini.KEY_LOCAL_FOLDER, dialog.FileName);
                }
            }

            setButtonsToStandard();
        }


        private void btn_choose_tmp_folder_Click(object sender, RoutedEventArgs e)
        {
            if (!Ini.hasFlag(Ini.KEY_FLAG_TEMP_DATA_NOTICE))
            {
                new MaterialDialog("Important", "All the files from the project folder will be compressed inside the temporary folder. Make sure that you have enough storage available.", 300).ShowDialog();
                Ini.setFlag(Ini.KEY_FLAG_TEMP_DATA_NOTICE);
            }

            var dialog = new CommonOpenFileDialog();
            dialog.AddToMostRecentlyUsedList = false;
            dialog.IsFolderPicker = true;
            dialog.Multiselect = false;
            dialog.Title = "Select a temporary folder";

            if(!String.IsNullOrWhiteSpace(ChosenTempFolder) && Directory.Exists(ChosenTempFolder))
            {
                dialog.InitialDirectory = ChosenTempFolder;
            }

            CommonFileDialogResult result = dialog.ShowDialog();

            if (result == CommonFileDialogResult.Ok && Directory.Exists(dialog.FileName))
            {
                if(ValidateFolders(ChosenProjectFolder, dialog.FileName))
                {
                    tb_chosen_temp_folder.Text = dialog.FileName;
                    ChosenTempFolder = dialog.FileName;
                    Ini.write(Ini.KEY_LOCAL_TEMP_FOLDER, dialog.FileName);
                }
            }
            
            setButtonsToStandard();
        }

        // Validate selected folders
        private bool ValidateFolders(string ProjectFolder, string TempFolder)
        {
            // Conditions that must be met for the TempFolder:
            // x TempFolder cannot be a subfolder of ProjectFolder
            // x TempFolder should to be equal to or shorter than ProjectFolder (So that there is no file that can't be written because its path would be longer than the maximum)
            // x TempFolder has to have at least 10 GB of free storage


            // Check if TempFolder has enough free space left
            if (!String.IsNullOrWhiteSpace(ProjectFolder) && !String.IsNullOrWhiteSpace(TempFolder))
            {
                foreach (DriveInfo drive in DriveInfo.GetDrives())
                {
                    if (drive.IsReady && drive.Name == Path.GetPathRoot(TempFolder))
                    {
                        // 10 GB in Bytes
                        long NeccessaryFreeSpace = 10737418240;
                        if (drive.AvailableFreeSpace <= NeccessaryFreeSpace)
                        {
                            long NeccessaryFreeSpaceInGigabyte = (long)Math.Round((double)NeccessaryFreeSpace / 1024 / 1024 / 1024);
                            new MaterialDialog("Error", "Your temporary directory doesn't have enough space left.\nIt needs at least " + NeccessaryFreeSpaceInGigabyte + " GB").ShowDialog();
                            return false;
                        }
                    }
                }
            }

            if(!String.IsNullOrWhiteSpace(ProjectFolder) && !String.IsNullOrWhiteSpace(TempFolder))
            {
                // Check if TempFolder is a subdirectory of ProjectFolder
                if (TempFolder.Contains(ProjectFolder))
                {
                    new MaterialDialog("Error", "The temporary directory cannot be a subdirectory of the project folder.").ShowDialog();
                    return false;
                }
                // Check if TempFolder has a longer path than ProjectFolder
                if (TempFolder.Length >= ProjectFolder.Length)
                {
                    new MaterialDialog("Warning", "The path to your temporary directory is longer than the path to your project folder.\nIf you run into problems, change the temporary directory to something shorter, for example C:\\tmp\\").ShowDialog();
                }
            }

            return true;
        }

        static long GetDirectorySize(string parentDir)
        {
            long totalFileSize = 0;

            string[] dirFiles = Directory.GetFiles(parentDir, "*.*",
                                    System.IO.SearchOption.AllDirectories);

            foreach (string fileName in dirFiles)
            {
                // Use FileInfo to get length of each file.
                FileInfo info = new FileInfo(fileName);
                totalFileSize = totalFileSize + info.Length;
            }
            return totalFileSize;// String.Format(new FileSizeFormatProvider(), "{0:fs}", totalFileSize);
        }

        private void btn_upload_update_Click(object sender, RoutedEventArgs e)
        {
            StartProgress();
        }

        private void btn_cancel_upload_Click(object sender, RoutedEventArgs e)
        {
            StopProgress();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            StopProgress();
        }

        private void btn_close_window_Click(object sender, RoutedEventArgs e)
        {
            RaiseCloseEvent();
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
            }
        }

        void RaiseCloseEvent()
        {
            RoutedEventArgs newEventArgs = new RoutedEventArgs(MainWindow.CloseEvent);
            RaiseEvent(newEventArgs);
        }

        private void CloseAnimation_Completed(object sender, EventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void AppBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void btn_authenticate_Click(object sender, RoutedEventArgs e)
        {
            loadCredentials(true);
        }

        private void btn_test_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Remo
        }

        private int returnLatestVersion(dynamic db)
        {
            dynamic releases = db.game_info.lxp.Releases;
            return releases.Count;
        }

        public dynamic addRelease(dynamic db, GameInfo gi)
        {
            string JsonPath = Path.Combine(ChosenTempFolder, "db.json");
            var test = File.ReadAllText(JsonPath);
            dynamic CurrentDatabase = JsonConvert.DeserializeObject(test);

            int NewVersion = db.game_info.lxp.Releases.Count;


            JObject db_new = new JObject(CurrentDatabase);
            JObject new_release = JObject.Parse(JsonConvert.SerializeObject(gi));

            db_new["game_info"]["lxp"]["Releases"].Last.AddAfterSelf(new_release);

            
            File.WriteAllText(JsonPath, JsonConvert.SerializeObject(db_new));

            return db_new;
        }
    }
}
