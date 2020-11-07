using System;
using System.Threading;
using System.Configuration;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Util.Store;
using Google.Apis.Upload;
using Google.Apis.Services;
using GoogleFile = Google.Apis.Drive.v3.Data.File;
using Ionic.Zip;

namespace DCTR_Cliest
{
    class GoogleDrive
    {
        static public string folderName = "DCTRsystem";
        static public string idGoogleFolder = ConfigurationSettings.AppSettings["idGoogleFolder"].ToString();
        static public string localFolder = Cliest.appData + "\\" + folderName;
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

            string credentials = localFolder + "\\client_secret.json";

            if (!System.IO.File.Exists(credentials))
            {
                throw new Exception("Configuration file not found\n\n" + credentials);
            }

            var stream = new FileStream(credentials, FileMode.Open, FileAccess.Read);

            try
            {
                var client = new System.Net.WebClient();
                client.OpenRead("http://google.com/generate_204");
            }
            catch
            {
                throw new Exception("Internet OFF");
            }

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
                throw new Exception(ex.Message);
            }
            return GFileList;
        }

        static private async System.Threading.Tasks.Task<string> SendFile(DriveService driveService, IsolatedStorageFile storage, string fileName)
        {
            FilesResource.CreateMediaUpload requestFl;

            GoogleFile body = new GoogleFile();
            body.Name = System.IO.Path.GetFileName(fileName);
            body.Description = "Test upload v4";
            body.MimeType = "application/zip";
            body.Parents = new List<string>() { GoogleDrive.idGoogleFolder };

            // Logger.Log.Info("Send file: " + nameFile);
            IsolatedStorageFileStream stream2 = new IsolatedStorageFileStream(fileName, FileMode.Open, storage);

            requestFl = driveService.Files.Create(body, stream2, "application/zip");
            IUploadProgress response = await requestFl.UploadAsync();
            stream2.Close();

            if (response.Exception != null)
            {
                Cliest.eventLog.WriteEntry($"{response.Exception.Message}, file: {fileName}" , EventLogEntryType.Warning, 785);
                ListIsoStorageFile.Add(fileName);
                throw new Exception(response.Exception.Message);
            }
            // Logger.Log.Info("Send file " + nameFile + " complited");
            var Rfile = requestFl.ResponseBody;
            storage.DeleteFile(fileName);
            return Rfile.Id;
        }

        static public async System.Threading.Tasks.Task<List<string>> UploadFilesAsync(DriveService driveService)
        {
            List<string> UploadListFileId = new List<string>();

            IsolatedStorageFile machine = IsolatedStorageFile.GetMachineStoreForAssembly();

            long epochTicks = new DateTime(1970, 1, 1).Ticks;
            long unixTime = ((DateTime.UtcNow.Ticks - epochTicks) / TimeSpan.TicksPerSecond);

            string ZipFileName = $"package_{unixTime}.zip";
            string idGoogleFile = "";

            using (IsolatedStorageFileStream fsZip = new IsolatedStorageFileStream(ZipFileName, FileMode.Create, machine))
            {
                using (ZipOutputStream s = new ZipOutputStream(fsZip))
                {
                    while (ListIsoStorageFile.ListFile.Count > 0)
                    {
                        string nameFile = ListIsoStorageFile.Get();

                        if (nameFile.EndsWith(".zip"))
                        {
                            idGoogleFile = await SendFile(driveService, machine, nameFile);
                            UploadListFileId.Add(idGoogleFile);
                            Cliest.eventLog.WriteEntry($"Resend {nameFile} ({idGoogleFile})", EventLogEntryType.Warning, 786);
                            continue;
                        }

                        try
                        {
                            IsolatedStorageFileStream stream = new IsolatedStorageFileStream(nameFile, FileMode.Open, machine);

                            s.PutNextEntry(nameFile);
                            byte[] buffer = ReadToEnd(stream);
                            s.Write(buffer, 0, buffer.Length);
                            stream.Close();
                            UploadListFileId.Add(nameFile);
                            machine.DeleteFile(nameFile);

                        }
                        catch (Exception ex)
                        {
                            Cliest.eventLog.WriteEntry($"{ex.Message} ({nameFile}), count: {ListIsoStorageFile.ListFile.Count}", EventLogEntryType.Error, 781);
                            ListIsoStorageFile.Add(nameFile);
                        }
                    }

                }
            }

            idGoogleFile = await SendFile(driveService, machine, ZipFileName);

            UploadListFileId.Add(idGoogleFile);

            return UploadListFileId;
        }

        public static byte[] ReadToEnd(System.IO.Stream stream)
        {
            long originalPosition = 0;

            if (stream.CanSeek)
            {
                originalPosition = stream.Position;
                stream.Position = 0;
            }

            try
            {
                byte[] readBuffer = new byte[4096];

                int totalBytesRead = 0;
                int bytesRead;

                while ((bytesRead = stream.Read(readBuffer, totalBytesRead, readBuffer.Length - totalBytesRead)) > 0)
                {
                    totalBytesRead += bytesRead;

                    if (totalBytesRead == readBuffer.Length)
                    {
                        int nextByte = stream.ReadByte();
                        if (nextByte != -1)
                        {
                            byte[] temp = new byte[readBuffer.Length * 2];
                            Buffer.BlockCopy(readBuffer, 0, temp, 0, readBuffer.Length);
                            Buffer.SetByte(temp, totalBytesRead, (byte)nextByte);
                            readBuffer = temp;
                            totalBytesRead++;
                        }
                    }
                }

                byte[] buffer = readBuffer;
                if (readBuffer.Length != totalBytesRead)
                {
                    buffer = new byte[totalBytesRead];
                    Buffer.BlockCopy(readBuffer, 0, buffer, 0, totalBytesRead);
                }
                return buffer;
            }
            finally
            {
                if (stream.CanSeek)
                {
                    stream.Position = originalPosition;
                }
            }
        }

    }
}
