using Google.Apis.Auth.OAuth2;
using Google.Apis.Download;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using GoogleDriveService.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace GoogleDriveService.Service
{
    public class GoogleDriveServices
    {
        public static string[] Scopes = { DriveService.Scope.Drive };

        public static DriveService GetService(string keyFilePath)
        {
            // File existance check
            if (!System.IO.File.Exists(keyFilePath))
            {
                Console.WriteLine("File not found");
                return null;
            }

            //var certificate = new X509Certificate2(keyFilePath, "", X509KeyStorageFlags.Exportable);
            GoogleCredential credential;
            using (var stream = new FileStream(keyFilePath, FileMode.Open, FileAccess.Read))
            {
                credential = GoogleCredential.FromStream(stream)
                    .CreateScoped(Scopes);
            }
            //UserCredential credential;

            //using(var stream = new FileStream(@"D:\credentials.json", FileMode.Open, FileAccess.Read))
            //{
            //    String folderPath = @"D:\";
            //    String filePath = Path.Combine(folderPath, "DriveServiceCredentials.json");

            //    credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
            //        GoogleClientSecrets.Load(stream).Secrets,
            //        Scopes,
            //        "user",
            //        CancellationToken.None,
            //        new FileDataStore(filePath, true)).Result;
            //}

            //ServiceAccountCredential credential = new ServiceAccountCredential(
            //    new ServiceAccountCredential.Initializer("drive-client@drive-client-283005.iam.gserviceaccount.com")
            //    {
            //        Scopes = Scopes
            //    }.FromCertificate(certificate));

            DriveService service = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "Prokoushol"
            });

            return service;
        }

        // List All files
        public static List<GoogleDriveFile> GetDriveFiles(string path)
        {
            DriveService service = GetService(path);
            FilesResource.ListRequest request = service.Files.List();
            request.Q = "mimeType!='application/vnd.google-apps.folder'";
            request.Fields = "files(id, name, size, version, createdTime, parents, webViewLink)";

            //Get file list.
            IList<Google.Apis.Drive.v3.Data.File> files = request.Execute().Files;
            List<GoogleDriveFile> fileList = new List<GoogleDriveFile>();

            if(files != null && files.Count > 0)
            {
                foreach(var file in files)
                {
                    GoogleDriveFile googleDriveFile = new GoogleDriveFile
                    {
                        Id = file.Id,
                        Name = file.Name,
                        Size = file.Size,
                        Version = file.Version,
                        CreatedTime = file.CreatedTime,
                        webViewLink = file.WebViewLink
                    };

                    fileList.Add(googleDriveFile);
                }
            }

            return fileList;
        }

        // File Upload Method
        public static void FileUpload(HttpPostedFileBase file, string keypath)
        {
            if(file != null && file.ContentLength > 0)
            {
                DriveService service = GetService(keypath);

                string path = Path.Combine(HttpContext.Current.Server.MapPath("~/GoogleDriveFiles/"), file.FileName);
                file.SaveAs(path);

                var fileMetaData = new Google.Apis.Drive.v3.Data.File()
                {
                    Name = Path.GetFileName(path),
                    MimeType = MimeMapping.GetMimeMapping(path),
                    Parents = new List<string>
                    {
                        "1RKlyrZOCWnQuh0WV-wuc270n7TQiNdlT"
                    }
                };
                FilesResource.CreateMediaUpload request;

                using (var stream = new FileStream(path, FileMode.Open))
                {
                    request = service.Files.Create(fileMetaData, stream, fileMetaData.MimeType);
                    request.Fields = "id";
                    request.Upload();
                }

                var fileInfo = request.ResponseBody;
                System.IO.File.Delete(path);
            }
        }

        // File Download Method
        public static string DownloadGoogleFile(string fileId, string path)
        {
            DriveService service = GetService(path);

            string folderPath = @"D:\DriveFiles";
            FilesResource.GetRequest request = service.Files.Get(fileId);

            string fileName = request.Execute().Name;
            string filePath = Path.Combine(folderPath, fileName);

            MemoryStream stream = new MemoryStream();

            request.MediaDownloader.ProgressChanged += (Google.Apis.Download.IDownloadProgress progress) =>
            {
                switch (progress.Status)
                {
                    case DownloadStatus.Downloading:
                        {
                            //long bytes = progress.BytesDownloaded;
                            break;
                        }
                    case DownloadStatus.Completed:
                        {
                            SaveStream(stream, filePath);
                            break;
                        }
                    case DownloadStatus.Failed:
                        {
                            break;
                        }
                }
            };
            request.Download(stream);

            return filePath;
        }

        private static void SaveStream(MemoryStream stream, string filePath)
        {
            using (FileStream file = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite))
            {
                stream.WriteTo(file);
            }
        }

        public static void DeleteFile(GoogleDriveFile file, string path)
        {
            DriveService service = GetService(path);
            try
            {
                if(service == null)
                {
                    throw new Exception("Service offline");
                }
                if(file == null)
                {
                    throw new Exception(file.Id);
                }

                service.Files.Delete(file.Id).Execute();
            }
            catch (Exception e)
            {
                throw new Exception(e.GetBaseException().Message);
            }
        }
    }
}
