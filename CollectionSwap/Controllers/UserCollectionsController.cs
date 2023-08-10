using CollectionSwap.Models;
using Microsoft.AspNet.Identity;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;

namespace CollectionSwap.Controllers
{
    public class UserCollectionsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        public ActionResult Index()
        {
            return View();
        }

        [Authorize]
        public ActionResult Create(int? id)
        {
            Collection selectedCollection = db.Collections.Find(id);

            ViewBag.SelectedCollection = selectedCollection;
            ViewBag.AvailableCollections = db.Collections.ToList();
            return View();
        }

        [Authorize]
        [HttpPost]
        public ActionResult Create(int id, int[] quantity)
        {
            if (ModelState.IsValid)
            {
                string userId = User.Identity.GetUserId();
                UserCollection.Create(id, quantity, db, userId);
            }

            return RedirectToAction("Index", "Manage");
        }

        [Authorize]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return RedirectToAction("Index", "Manage");
            }

            UserCollection userCollection = db.UserCollections.Include("User").FirstOrDefault(uc => uc.Id == id);

            if (User.Identity.GetUserId() != userCollection.User.Id)
            {
                return RedirectToAction("Index", "Manage");
            }

            UserCollectionModel model = new UserCollectionModel
            {
                Collection = db.Collections.Find(userCollection.CollectionId),
                UserCollection = userCollection
            };

            ViewBag.Status = TempData["Success"];
            return View(model);
        }

        //[HttpPost]
        //[Authorize]
        //public ActionResult Edit(UserCollection userCollection, string propertyChanged)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        TempData["Success"] = userCollection.Update(db, propertyChanged);
        //        return RedirectToAction("Edit");
        //    }

        //    return RedirectToAction("Index", "Manage");
        //}

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int? userCollectionId)
        {
            var userCollection = db.UserCollections.Find(userCollectionId);
            TempData["Success"] = userCollection.Delete(db);
            return RedirectToAction("Index", "Manage");
        }
    }
}