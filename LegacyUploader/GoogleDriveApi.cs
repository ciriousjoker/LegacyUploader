using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v2;
using Google.Apis.Drive.v2.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.Windows;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Upload;
using static Google.Apis.Drive.v2.FilesResource;
using System.ComponentModel;
using System.Net;
using static LegacyManagerUploader.MainWindow;

namespace LegacyManagerUploader
{
    class GoogleDriveApi
    {
        static string[] Scopes = { DriveService.Scope.Drive };

        public static string CREDENTIAL_STORE = Path.Combine(".credentials", "drive_credentials.json");

        // MimeTypes
        static string MIME_7ZIP = "application/x-7z-compressed";
        static string MIME_JSON = "application/json";


        // WaitHandler to be able to force-cancel uploads
        public static AutoResetEvent stopWaitHandle = new AutoResetEvent(false);



        // Upload functions
        public static async void UploadGame(DriveService service, string file_path, Google.Apis.Drive.v2.Data.File _folder, Action<IUploadProgress, BackgroundWorker, string, long> onFileUploadProgressChanged, Action<Google.Apis.Drive.v2.Data.File, BackgroundWorker> onFileUploaded, BackgroundWorker sendingworker, string file_id = null)
        {
            if (!CheckInternetConnection()) { sendingworker.ReportProgress(0, new StatusHolder() { txt = "Not connected to the internet. Aborted.", progress = -1 }); sendingworker.CancelAsync(); return; }

            try
            {
                // Reset the WaitHandler
                stopWaitHandle.Reset();

                // Load the file
                var uploadStream = new FileStream(file_path, FileMode.Open, FileAccess.Read);

                // Check if the file should be overwritten or newly created
                if (file_id != null)
                {
                    // Update existing file
                    UpdateMediaUpload uploadRequest = service.Files.Update(
                    new Google.Apis.Drive.v2.Data.File
                    {
                        Title = Path.GetFileName(file_path),
                        Parents = new List<ParentReference>() { new ParentReference() { Id = _folder.Id } }
                    },
                    file_id,
                    uploadStream,
                    MIME_7ZIP);

                    uploadRequest.ChunkSize = ResumableUpload<InsertRequest>.MinimumChunkSize;

                    uploadRequest.ProgressChanged += (progress) => onFileUploadProgressChanged(progress, sendingworker, Path.GetFileNameWithoutExtension(file_path), new FileInfo(file_path).Length);
                    uploadRequest.ResponseReceived += (file) => onFileUploaded(file, sendingworker);

                    var task = uploadRequest.UploadAsync();
                    await task.ContinueWith(t =>
                    {
                        uploadStream.Dispose();
                    });
                }
                else
                {
                    // Create new file
                    InsertMediaUpload uploadRequest = service.Files.Insert(
                    new Google.Apis.Drive.v2.Data.File
                    {
                        Title = Path.GetFileName(file_path),
                        Parents = new List<ParentReference>() { new ParentReference() { Id = _folder.Id } }
                    },
                    uploadStream,
                    MIME_7ZIP);
                    uploadRequest.ChunkSize = ResumableUpload<InsertRequest>.MinimumChunkSize;

                    uploadRequest.ProgressChanged += (progress) => onFileUploadProgressChanged(progress, sendingworker, Path.GetFileNameWithoutExtension(file_path), new FileInfo(file_path).Length);
                    uploadRequest.ResponseReceived += (file) => onFileUploaded(file, sendingworker);

                    var task = uploadRequest.UploadAsync();
                    await task.ContinueWith(t =>
                    {
                        uploadStream.Dispose();
                    });
                }
                // Wait for whichever comes first: User aborts the task; Upload was successful
                stopWaitHandle.WaitOne();

                // Clear the local fileStream. Note that this will also abort any current upload tasks.
                uploadStream.Dispose();
            }
            catch (WebException e)
            {
                sendingworker.ReportProgress(0, new StatusHolder() { txt = "There are issues with the connection. Aborted.", progress = -1 });
                sendingworker.ReportProgress(0, new StatusHolder() { txt = "Error message: " + Environment.NewLine + e.Message, progress = -1 });
                sendingworker.CancelAsync();
            }
        }

        // TODO: Remove this function
        public static async void UploadPackage(DriveService service, string file_path, Google.Apis.Drive.v2.Data.File _folder, Action<IUploadProgress, BackgroundWorker, string, long> onFileUploadProgressChanged, Action<Google.Apis.Drive.v2.Data.File, BackgroundWorker> onFileUploaded, BackgroundWorker sendingworker, string file_id = null)
        {
            if (!CheckInternetConnection()) { sendingworker.ReportProgress(0, new StatusHolder() { txt = "Not connected to the internet. Aborted.", progress = -1 }); sendingworker.CancelAsync(); return; }

            try
            {
                // Reset the WaitHandler
                stopWaitHandle.Reset();

                // Load the file
                var uploadStream = new FileStream(file_path, FileMode.Open, FileAccess.Read);

                // Check if the file should be overwritten or newly created
                if (file_id != null)
                {
                    // Update existing file
                    UpdateMediaUpload uploadRequest = service.Files.Update(
                    new Google.Apis.Drive.v2.Data.File
                    {
                        Title = Path.GetFileName(file_path),
                        Parents = new List<ParentReference>() { new ParentReference() { Id = _folder.Id } }
                    },
                    file_id,
                    uploadStream,
                    MIME_7ZIP);

                    uploadRequest.ChunkSize = ResumableUpload<InsertRequest>.MinimumChunkSize;

                    uploadRequest.ProgressChanged += (progress) => onFileUploadProgressChanged(progress, sendingworker, Path.GetFileNameWithoutExtension(file_path), new FileInfo(file_path).Length);
                    uploadRequest.ResponseReceived += (file) => onFileUploaded(file, sendingworker);

                    var task = uploadRequest.UploadAsync();
                    await task.ContinueWith(t =>
                    {
                        uploadStream.Dispose();
                    });
                }
                else
                {
                    // Create new file
                    InsertMediaUpload uploadRequest = service.Files.Insert(
                    new Google.Apis.Drive.v2.Data.File
                    {
                        Title = Path.GetFileName(file_path),
                        Parents = new List<ParentReference>() { new ParentReference() { Id = _folder.Id } }
                    },
                    uploadStream,
                    MIME_7ZIP);
                    uploadRequest.ChunkSize = ResumableUpload<InsertRequest>.MinimumChunkSize;

                    uploadRequest.ProgressChanged += (progress) => onFileUploadProgressChanged(progress, sendingworker, Path.GetFileNameWithoutExtension(file_path), new FileInfo(file_path).Length);
                    uploadRequest.ResponseReceived += (file) => onFileUploaded(file, sendingworker);

                    var task = uploadRequest.UploadAsync();
                    await task.ContinueWith(t =>
                    {
                        uploadStream.Dispose();
                    });
                }
                // Wait for whichever comes first: User aborts the task; Upload was successful
                stopWaitHandle.WaitOne();

                // Clear the local fileStream. Note that this will also abort any current upload tasks.
                uploadStream.Dispose();
            } catch (WebException e)
            {
                sendingworker.ReportProgress(0, new StatusHolder() { txt = "There are issues with the connection. Aborted.", progress = -1 });
                sendingworker.ReportProgress(0, new StatusHolder() { txt = "Error message: " + Environment.NewLine + e.Message, progress = -1 });
                sendingworker.CancelAsync();
            }
        }

        public static string uploadJson(DriveService service, string file_path, Google.Apis.Drive.v2.Data.File _folder, string file_id = null)
        {
            try
            {
                if (!CheckInternetConnection()) { return null; }
                var uploadStream = new FileStream(file_path, FileMode.Open, FileAccess.Read);

                // Create or update the JsonTable
                if (file_id == null)
                {
                    // Create it
                    InsertMediaUpload uploadRequest = service.Files.Insert(
                        new Google.Apis.Drive.v2.Data.File
                        {
                            Title = Path.GetFileName(file_path),
                            Parents = new List<ParentReference>() { new ParentReference() { Id = _folder.Id } }
                        },
                        uploadStream,
                        MIME_JSON);
                    var task = uploadRequest.Upload();
                    uploadStream.Dispose();
                    return uploadRequest.ResponseBody.Id;
                }
                else
                {
                    // Update it
                    UpdateMediaUpload uploadRequest = service.Files.Update(
                        new Google.Apis.Drive.v2.Data.File
                        {
                            Title = Path.GetFileName(file_path),
                            Parents = new List<ParentReference>() { new ParentReference() { Id = _folder.Id } }
                        },
                        file_id,
                        uploadStream,
                        MIME_JSON);
                    var task = uploadRequest.Upload();
                    uploadStream.Dispose();
                    return uploadRequest.FileId;
                }
            }
            catch (WebException e)
            {
                throw new WebException("There are issues with the connection. Maybe try again later. Full error message: " + e.Message);
            }
        }
        
        // List all files in a directory
        // TODO: Remove all unneccessary packages by checking the filenames
        public static void listFiles(DriveService service)
        {
            if (!CheckInternetConnection()) { return; }
            // Define parameters of request.
            FilesResource.ListRequest listRequest = service.Files.List();
            listRequest.MaxResults = 10;

            // List files.
            IList<Google.Apis.Drive.v2.Data.File> files = listRequest.Execute().Items;
            if (files != null && files.Count > 0)
            {
                foreach (var file in files)
                {
                    MessageBox.Show("File found: " + file.Title + "\nId: " + file.Id);
                }
            }
            else
            {
                MessageBox.Show("No files found.");
            }
        }

        // Create a directory
        public static Google.Apis.Drive.v2.Data.File createDirectory(DriveService _service, string _title, string _description)
        {
            return createDirectory(_service, _title, _description, null);
        }
        
        public static Google.Apis.Drive.v2.Data.File createDirectory(DriveService _service, string _title, string _description, string _id)
        {
            if (!CheckInternetConnection()) { return null; }
            Google.Apis.Drive.v2.Data.File NewDirectory = null;

            // Create metaData for a new Directory
            var body = new Google.Apis.Drive.v2.Data.File();
            body.Title = _title;
            body.Description = _description;
            body.MimeType = "application/vnd.google-apps.folder";

            // TODO: Remove this
            /*
            body.UserPermission = new Permission
            {
                Type = "anyone",
                Role = "reader",
                WithLink = true
            };
            */

            try
            {
                if (_id != null)
                {
                    FilesResource.UpdateRequest request = _service.Files.Update(body, _id);
                    NewDirectory = request.Execute();
                } else
                {
                    FilesResource.InsertRequest request = _service.Files.Insert(body);
                    NewDirectory = request.Execute();
                }
            }
            catch (Exception e)
            {
                new MaterialDialog("An unexpected error occured", "It looks like something went wrong. If you have an idea, email me at ciriousjoker@gmail.com or open an issue over at Github!\n\n\n" + e.Message, 500).ShowDialog();
                return null;
            }

            insertPermission(_service, NewDirectory.Id);
            return NewDirectory;
        }

        public static void insertPermission(DriveService _service, String fileId)
        {
            Permission newpermission = new Permission
            {
                Type = "anyone",
                Role = "reader",
                WithLink = true
            };

            var test = _service.Permissions.Insert(newpermission, fileId).Execute();
    }

    // Authorization stuff
    public static DriveService CreateService()
        {
            var credentials = getCredentials();
            if (credentials == null) { return null; }
            return new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credentials,
                ApplicationName = MainWindow.AppName
            });
        }

        private static UserCredential getCredentials()
        {
            if(!CheckInternetConnection()) { return null; }

            try
            {
                using (var stream = new FileStream("client_secret.json", FileMode.Open, FileAccess.Read))
                {
                    try
                    {
                        UserCredential credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                                                        GoogleClientSecrets.Load(stream).Secrets,
                                                        Scopes,
                                                        "user",
                                                        CancellationToken.None,
                                                        new FileDataStore(CREDENTIAL_STORE, true)).Result;
                        return credential;
                    }
#pragma warning disable CS0168 // Variable is declared but never used
                    catch (Exception ignored)
#pragma warning restore CS0168 // Variable is declared but never used
                    {
                        return null;
                    }
                }
            } catch (Exception e)
            {
                new MaterialDialog("An unexpected error occured", "It looks like something went wrong. If you have an idea, email me at ciriousjoker@gmail.com or open an issue over at Github!\n\n\n" + e.Message, 500).ShowDialog();
                return null;
            }
        }

        // Check if online
        public static bool CheckInternetConnection()
        {
            WebClient client = new WebClient();
            try
            {
                using (client.OpenRead("http://www.google.com"))
                {
                }
                return true;
            }
            catch (WebException)
            {
                return false;
            }
        }
    }
}
