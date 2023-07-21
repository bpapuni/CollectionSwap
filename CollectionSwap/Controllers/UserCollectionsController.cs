using CollectionSwap.Models;
using System;
using System.Collections.Generic;
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

        public ActionResult Create(int? id)
        {
            Collection selectedCollection = db.Collections.Find(id);

            ViewBag.SelectedCollection = selectedCollection;
            ViewBag.Collections = db.Collections.ToList();
            return View();
        }
    }
}