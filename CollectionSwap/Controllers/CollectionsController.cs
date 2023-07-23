using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Web;
using System.Web.Mvc;
using CollectionSwap.Models;

namespace CollectionSwap.Controllers
{
    public class CollectionsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        public ActionResult Index()
        {
            return RedirectToAction("Index", "Manage");
        }

        [Authorize(Roles = "Admin")]
        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult Create(CreateCollection collection)
        {
            if (ModelState.IsValid)
            {
                if (collection.fileInput != null && collection.fileInput.ContentLength > 0)
                {
                    // Access the contents of the zip file
                    using (ZipArchive archive = new ZipArchive(collection.fileInput.InputStream, ZipArchiveMode.Read))
                    {
                        // Save the collection to the database so it is allocated an Id
                        Collection newCollection = new Collection()
                        {
                            Name = collection.Name,
                            Size = archive.Entries.Count
                        };
                        db.Collections.Add(newCollection);
                        db.SaveChanges();

                        // Specify the path where to save the uploaded file on the server
                        string extractPath = Server.MapPath("~/Collections/" + newCollection.Id);

                        // Ensure the target directory exists; create it if it doesn't
                        if (!Directory.Exists(extractPath))
                        {
                            Directory.CreateDirectory(extractPath);
                        }

                        int i = 1;

                        foreach (ZipArchiveEntry entry in archive.Entries)
                        {
                            // Ensure that the entry is a file, not a directory
                            if (!string.IsNullOrEmpty(entry.Name))
                            {
                                string extractedFilePath = Path.Combine(extractPath, i.ToString() + Path.GetExtension(entry.Name));
                                entry.ExtractToFile(extractedFilePath, true);

                                i++;
                            }
                        }
                    }

                    return RedirectToAction("Index", "Manage");
                }
            }
            return View(collection);
        }

        [Authorize(Roles = "Admin")]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return RedirectToAction("Index", "Manage");
            }

            Collection collection = db.Collections.Find(id);

            string path = Server.MapPath("~/Collections/" + collection.Id);
            string[] files = Directory.GetFiles(path);
            files = files.Select(fileName => Path.GetFileName(fileName)).ToArray();

            ViewBag.Items = files.OrderBy(f => f.Length);
            ViewBag.Status = TempData["Success"];
            ViewBag.ImageUrl = TempData["ImageUrl"];
            return View(collection);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult Edit(Collection collection)
        {
            if (ModelState.IsValid)
            {
                db.Entry(collection).State = EntityState.Modified;
                db.SaveChanges();
                TempData["Success"] = "Collection name updated successfully.";
                return RedirectToAction("Edit");
            }
            return View(collection);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult Delete(int? collectionId)
        {
            Collection collection = db.Collections.Find(collectionId);
            db.Collections.Remove(collection);
            db.SaveChanges();

            string directoryPath = Server.MapPath("~/Collections/" + collectionId);
            if (Directory.Exists(directoryPath))
            {
                // Delete the directory and its content (recursive: true)
                Directory.Delete(directoryPath, recursive: true);
            }

            return RedirectToAction("Index", "Manage");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult CreateItem(int? collectionId, HttpPostedFileBase fileInput)
        {
            if (fileInput != null && fileInput.ContentLength > 0)
            {
                string directoryPath = Server.MapPath("~/Collections/" + collectionId);
                string[] items = Directory.GetFileSystemEntries(directoryPath);
                string filePath = directoryPath + '/' + (items.Length + 1) + ".jpg";

                Image img = Image.FromStream(fileInput.InputStream);
                img.Save(filePath, ImageFormat.Jpeg);
            }

            return RedirectToAction("Edit/" + collectionId);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult EditItem(int? collectionId, int itemId, string fileName, HttpPostedFileBase fileInput)
        {
            if (fileInput != null && fileInput.ContentLength > 0)
            {
                string filePath = Server.MapPath("~/Collections/" + collectionId + '/' + fileName);
                fileInput.SaveAs(filePath);

                string cacheBuster = DateTime.UtcNow.Ticks.ToString();
                TempData["ImageUrl"] = new List<string> { itemId.ToString(), $"~/Collections/{collectionId}/{fileName}?time={cacheBuster}" };
            }

            return RedirectToAction("Edit/" + collectionId);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult DeleteItem(int? collectionId, string fileName)
        {
            string filePath = Server.MapPath("~/Collections/" + collectionId + '/' + fileName);
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }

            return RedirectToAction("Edit/" + collectionId);
        }
    }
}