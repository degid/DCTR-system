using System;
using System.Threading;
using System.Configuration;
using System.Collections.Generic;
using System.IO;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Util.Store;

namespace ServiceManager
{
    class GoogleDrive
    {
        static public UserCredential GetUserCredential()
        {
            //Scopes for use with the Google Drive API
            string[] scopes = new string[] { DriveService.Scope.Drive,
                                 DriveService.Scope.DriveFile};
            // From https://console.developers.google.com

            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string credentials = appData + "\\DCTRsystem\\client_secret.json";

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
            new FileDataStore("DCTRsystem")).Result;

            credential.GetAccessTokenForRequestAsync();
            return credential;
        }
    }
}
