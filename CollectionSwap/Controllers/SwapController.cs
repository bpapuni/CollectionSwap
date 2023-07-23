using CollectionSwap.Models;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace CollectionSwap.Controllers
{
    public class SwapController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        [Authorize]
        public ActionResult Index()
        {
            string UserId = User.Identity.GetUserId();
            SwapViewModel swapViewModel = new SwapViewModel
            {
                UserCollections = db.UserCollections.Where(uc => uc.UserId == UserId).ToList()
            };

            return View(swapViewModel);
        }


        [Authorize]
        [HttpPost]
        public ActionResult Index(int id)
        {
            //Collection selectedCollection = db.Collections.Find(id);

            //ViewBag.SelectedCollection = selectedCollection;
            //ViewBag.Collections = db.Collections.ToList();
            return View();
        }
    }
}