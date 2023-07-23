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
            ViewBag.Collections = db.Collections.ToList();
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
                    Name = "UserCollection",
                    UserId = User.Identity.GetUserId(),
                    CollectionId = id,
                    ItemIdsJSON = JsonConvert.SerializeObject(quantity)
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

            if (User.Identity.GetUserId() != userCollection.UserId)
            {
                return RedirectToAction("Index", "Manage");
            }

            string path = Server.MapPath("~/Collections/" + userCollection.CollectionId);
            string[] files = Directory.GetFiles(path);
            files = files.Select(fileName => Path.GetFileName(fileName)).ToArray();

            ViewBag.Items = files.OrderBy(f => f.Length);
            return View(userCollection);
        }

        [Authorize]
        [HttpPost]
        public ActionResult Edit(UserCollection userCollection)
        {
            if (ModelState.IsValid)
            {
                db.Entry(userCollection).State = EntityState.Modified;
                db.SaveChanges();
                return Json(new { success = true, message = "Data processed successfully." });
            }

            return View();
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