using System;
using System.Threading;
using System.Configuration;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Util.Store;
using Google.Apis.Upload;
using Google.Apis.Services;
using GoogleFile = Google.Apis.Drive.v3.Data.File;

namespace DCTR_Cliest
{
    class GoogleDrive
    {
        static public string folderName = "DCTRsystem";
        static public string idGoogleFolder = ConfigurationSettings.AppSettings["idGoogleFolder"].ToString();
        static public List<string> GFileList = new List<string>();

        static GoogleDrive()
        {

        }

        static public UserCredential GetUserCredential()
        {
            //Scopes for use with the Google Drive API
            string[] scopes = new string[] { DriveService.Scope.Drive,
                                 DriveService.Scope.DriveFile};
            // From https://console.developers.google.com

            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string credentials = appData + "\\" + folderName + "\\client_secret.json";

            if (!System.IO.File.Exists(credentials))
            {
                throw new Exception("Configuration file not found\n\n" + credentials);
            }

            var stream = new FileStream(credentials, FileMode.Open, FileAccess.Read);

            // here is where we Request the user to give us access, or use the Refresh Token that was previously stored in %AppData%
            var credential = GoogleWebAuthorizationBroker.AuthorizeAsync(GoogleClientSecrets.Load(stream).Secrets,
            scopes,
            Environment.UserName,
            CancellationToken.None,
            new FileDataStore(folderName)).Result;

            credential.GetAccessTokenForRequestAsync();
            return credential;
        }

        // Create the service
        static public DriveService CreateService(UserCredential credential)
        {
            try
            {
                if (credential == null)
                    throw new ArgumentNullException("credential");

                return new DriveService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "DCTR system",
                });
            }
            catch (Exception ex)
            {
                throw new Exception("Get Drive service failed.", ex);
            }

        }

        static public async System.Threading.Tasks.Task<List<string>> GetListAsync(DriveService driveService)
        {
            List<string> GFileList = new List<string>();
            try
            {
                FilesResource.ListRequest request = driveService.Files.List();
                request.Q = String.Format("'{0}' in parents", GoogleDrive.idGoogleFolder); //"trashed = false";
                FileList filesFeed = await request.ExecuteAsync();

                while (filesFeed.Files != null)
                {
                    foreach (GoogleFile item in filesFeed.Files)
                    {
                        //Files.Add(item);
                        //Console.WriteLine(item.Name);
                        GFileList.Add(item.Name);
                    }

                    // We will know we are on the last page when the next page token is
                    // null.
                    // If this is the case, break.
                    if (filesFeed.NextPageToken == null)
                    {
                        break;
                    }

                    // Prepare the next page of results
                    request.PageToken = filesFeed.NextPageToken;

                    // Execute and process the next page request
                    filesFeed = await request.ExecuteAsync();
                }
            }
            catch (Exception ex)
            {
                // In the event there is an error with the request.
                Console.WriteLine(ex.Message);
            }
            return GFileList;
        }

        static public async System.Threading.Tasks.Task<List<string>> UploadFilesAsync(DriveService driveService, List<string> ListLocalFile)
        {
            List<string> UploadListFileId = new List<string>();

            FilesResource.CreateMediaUpload requestFl;
            IsolatedStorageFile machine = IsolatedStorageFile.GetMachineStoreForAssembly();

            foreach (string nameFile in ListLocalFile)
            {
                //Console.WriteLine(">>" + nameFile);

                GoogleFile body = new GoogleFile();
                body.Name = System.IO.Path.GetFileName(nameFile);
                body.Description = "Test upload v1";
                body.MimeType = "application/json";
                body.Parents = new List<string>() { GoogleDrive.idGoogleFolder };

                IsolatedStorageFileStream stream = new IsolatedStorageFileStream(nameFile, FileMode.Open, machine);

                requestFl = driveService.Files.Create(body, stream, "application/json");

                //requestFl.Upload();
                IUploadProgress response = await requestFl.UploadAsync();//.ConfigureAwait(false);
                stream.Close();

                var Rfile = requestFl.ResponseBody;
                string m_fileId = requestFl.ResponseBody?.Id;

                if (response.Exception != null)
                {
                    //result.Error = new SerializableAPIError { ErrorMessage = response.Exception.Message };
                    Console.WriteLine("ErrorMessage: " + response.Exception.Message);
                }
                else
                {
                    //Console.WriteLine("File ID: " + Rfile.Id);
                    UploadListFileId.Add(Rfile.Id);
                    //machine.DeleteFile(nameFile);
                    Console.WriteLine("local: " + nameFile);
                }
            }
            return UploadListFileId;
        }
    }
}
