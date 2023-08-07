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
using CollectionSwap.Helpers;
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
        public ActionResult Create(ManageCollectionsViewModel model)
        {
            if (ModelState.IsValid)
            {
                Collection.Create(model.NewCollection, db);
                //return RedirectToAction("LoadPartial", "Manage", new { partialName = "_ManageCollections" });
                return Json(new { Success = true, Reload = true });
            }

            string viewHtml = Helper.RenderViewToString(ControllerContext, "~/Views/Manage/_CreateCollection.cshtml", model, true);
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);

            return Json(new { Success = false, Errors = errors, PartialView = viewHtml });
        }

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //[Authorize(Roles = "Admin")]
        //public ActionResult Edit(int collectionId)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return Json(new { success = false, Errors = ModelState.Values.SelectMany((v, index) => v.Errors.Select(e => new { Index = index, Error = e.ErrorMessage })) });
                
        //    }

        //    Collection collection = db.Collections.Find(collectionId);
        //    collection.Update(db);

        //    var model = new ManageCollectionsViewModel
        //    {
        //        Collections = db.Collections.ToList(),
        //        NewCollection = new CreateCollection { }
        //    };

        //    return PartialView("~/Views/Manage/_ManageCollections.cshtml", model);
        //}

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