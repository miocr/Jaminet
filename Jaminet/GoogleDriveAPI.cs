﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using log4net;

using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;


namespace Jaminet
{
    class GoogleDriveAPI
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(GoogleDriveAPI));

        // If modifying these scopes, delete your previously saved credentials
        // at ~/.credentials/drive-dotnet-quickstart.json
        static readonly string[] Scopes = { DriveService.Scope.DriveReadonly };
        static string ApplicationName = "Drive API .NET Quickstart";
        private DriveService service;


        public GoogleDriveAPI()
        {
        }

        public void DownloadFile(string fileId, string fileName, string mimeType)
        {
            service = CreateService();

            using (FileStream fs = new FileStream(fileName, FileMode.Create, FileAccess.Write))
            {
                Console.WriteLine("Downloading GoogleDrive fileId '{0}' to '{1}'", fileId, fileName);
                FilesResource.ExportRequest request = service.Files.Export(fileId, mimeType);
                request.Download(fs);

                log.InfoFormat("Downloaded fileId '{0}' to '{1}' from GoogleDrive",fileId, fileName);

            }
        }

        public DriveService CreateService()
        {
            UserCredential credential = null;
            try
            {
                using (var stream = new FileStream("client_secret.json", FileMode.Open, FileAccess.Read))
                {
                    string credPath = "./";
                    credPath = Path.Combine(credPath, ".credentials/google-drive-oauth.json");


                    credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                        GoogleClientSecrets.Load(stream).Secrets,
                        Scopes,
                        "user",
                        CancellationToken.None,
                        new FileDataStore(credPath, true)).Result;

                    //Console.WriteLine("Credential file saved to: " + credPath);
                }
            }
            catch (Exception exc)
            {
                Console.WriteLine("Create GoogleDisk service Exception:{0}", exc.Message);
                Environment.Exit(-1);
            }

            if (credential != null)
            {
                // Create Drive API service.
                service = new DriveService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = ApplicationName,
                });
            }

            return service;
        }

    }
}
