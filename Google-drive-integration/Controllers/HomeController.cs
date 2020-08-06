using GoogleDriveService.Model;
using GoogleDriveService.Service;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Google_drive_integration.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        [HttpGet]
        public ActionResult GetGoogleDriveFiles()
        {
            return View(GoogleDriveServices.GetDriveFiles(Server.MapPath("~/drive-client.json")));
        }

        [HttpPost]
        public ActionResult DeleteFile(GoogleDriveFile file)
        {
            GoogleDriveServices.DeleteFile(file, Server.MapPath("~/drive-client.json"));
            return RedirectToAction("GetGoogleDriveFiles");
        }

        [HttpPost]
        public ActionResult UploadFile(HttpPostedFileBase file)
        {
            GoogleDriveServices.FileUpload(file, Server.MapPath("~/drive-client.json"));
            return RedirectToAction("GetGoogleDriveFiles");
        }

        [HttpPost]
        public void DownloadFile(string id)
        {
            string FilePath = GoogleDriveServices.DownloadGoogleFile(id, Server.MapPath("~/drive-client.json"));
            Response.ContentType = "application/zip";
            Response.AddHeader("Content-Disposition", "attachment; filename=" + Path.GetFileName(FilePath));
            Response.WriteFile(@"D:\DriveFiles\" + Path.GetFileName(FilePath));
            Response.End();
            Response.Flush();
        }
    }
}