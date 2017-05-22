using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Xml;

namespace ExampleAttacks.Controllers
{
    public class MainController : Controller
    {
        //
        // GET: /Main/

        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public string Xxe()
        {
            string result = string.Empty;
            Response.ContentType = "text/plain";

            XmlDocument inputDocument = new XmlDocument();
            MemoryStream outputStream = new MemoryStream();
            XmlTextWriter outputWriter = new XmlTextWriter(outputStream, Encoding.Unicode);
            outputWriter.Formatting = Formatting.Indented;
            try
            {
                if (Request.Files.Count >= 1)
                {
                    HttpPostedFileBase file = Request.Files[0];
                    System.IO.Stream fileContentStream = file.InputStream;
                    XmlReader reader = new XmlTextReader(fileContentStream);
                    inputDocument.Load(reader);
                    result = inputDocument.InnerText;
                }
            }
            catch (Exception ex)
            {
                return ex.Message;  // YOLO
            }
            return result;
        }

        [HttpPost]
        public string Ssrf(string uri, string method, string postData)
        {
            WebRequest request = WebRequest.Create(uri);
            request.Method = method.ToUpper();
            if (String.Equals(method, "POST", StringComparison.OrdinalIgnoreCase))
            {
                byte [] postDataBytes = new ASCIIEncoding().GetBytes(postData);
                Stream stream = request.GetRequestStream();
                stream.Write(postDataBytes, 0, postDataBytes.Length);
                stream.Close();
            }
            WebResponse response = request.GetResponse();
            Stream responseStream = response.GetResponseStream();
            StreamReader responseStreamReader = new StreamReader(responseStream);
            response.Close();
            return responseStreamReader.ReadToEnd();
        }

        [HttpPost]
        public JsonResult PathTraversal(string user, string file)
        {
            string basePath = Server.MapPath("~/Files");
            if (!String.IsNullOrEmpty(user))
            {
                string directoryPath = Path.Combine(basePath, user);

                if (!String.IsNullOrEmpty(file))
                {
                    string fullPath = Path.Combine(directoryPath, file);
                    if (System.IO.File.Exists(fullPath))
                    {
                        FileStream fileStream = new FileStream(fullPath, FileMode.Open);
                        StreamReader fileStreamReader = new StreamReader(fileStream);
                        string fileContent = fileStreamReader.ReadToEnd();
                        fileStream.Close();
                        return Json(fileContent);
                    }
                }
                else if (Directory.Exists(directoryPath))
                {
                    IEnumerable<string> entries = Directory.EnumerateFileSystemEntries(directoryPath);
                    IEnumerable<string> fileNames = entries.Select<string, string>(entry => Path.GetFileName(entry));
                    return Json(fileNames);
                }
            }
            return Json("");
        }
    }
}
