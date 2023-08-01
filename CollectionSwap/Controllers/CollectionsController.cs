using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using Newtonsoft.Json;

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
                Collection.Create(collection, db);
                return RedirectToAction("Index", "Manage");
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

            ViewBag.Status = TempData["Success"];
            return View(collection);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult Edit(Collection collection)
        {
            if (ModelState.IsValid)
            {
                collection.Update(db);
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
            collection.Delete(db);

            return RedirectToAction("Index", "Manage");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult CreateItem(int collectionId, HttpPostedFileBase fileInput)
        {
            var collection = db.Collections.Find(collectionId);
            collection.AddItem(fileInput, db);

            return RedirectToAction("Edit/" + collectionId);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult EditItem(int collectionId, int itemId, string fileName, HttpPostedFileBase fileInput)
        {
            var collection = db.Collections.Find(collectionId);
            collection.EditItem(itemId, fileName, fileInput, db);

            return RedirectToAction("Edit/" + collectionId);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult DeleteItem(int? collectionId, string fileName)
        {
            var collection = db.Collections.Find(collectionId);
            collection.DeleteItem(fileName, db);

            return RedirectToAction("Edit/" + collectionId);
        }

        //[HttpPost]
        //[Authorize(Roles = "Admin")]
        //public void Refresh(int id)
        //{
        //    Collection collection = db.Collections.Find(id);
        //    collection.Refresh(db);
        //}
    }
}