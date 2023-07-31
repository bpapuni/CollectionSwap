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
                UserCollection newUserCollection = new UserCollection()
                {
                    Name = db.Collections.Find(id).Name,
                    User = db.Users.Find(User.Identity.GetUserId()),
                    CollectionId = id,
                    ItemCountJSON = JsonConvert.SerializeObject(quantity)
                };
                db.UserCollections.Add(newUserCollection);
                db.SaveChanges();
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

            UserCollection userCollection = db.UserCollections.Find(id);

            if (User.Identity.GetUserId() != userCollection.User.Id)
            {
                return RedirectToAction("Index", "Manage");
            }

            UserCollectionEditViewModel model = new UserCollectionEditViewModel
            {
                Collection = db.Collections.Find(userCollection.CollectionId),
                UserCollection = userCollection
            };

            ViewBag.Status = TempData["Success"];
            return View(model);
        }

        [HttpPost]
        [Authorize]
        public ActionResult Edit(UserCollection userCollection, string propertyChanged)
        {
            if (ModelState.IsValid)
            {
                db.Entry(userCollection).State = EntityState.Modified;
                db.SaveChanges();
                switch (propertyChanged)
                {
                    case "name":
                        TempData["Success"] = "Collection name updated successfully.";
                        break;
                    default:
                        break;
                }
                return RedirectToAction("Edit");

            }

            return RedirectToAction("Index", "Manage");
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int? userCollectionId)
        {
            UserCollection userCollection = db.UserCollections.Find(userCollectionId);
            db.UserCollections.Remove(userCollection);
            db.SaveChanges();
            return RedirectToAction("Index", "Manage");
        }
    }
}