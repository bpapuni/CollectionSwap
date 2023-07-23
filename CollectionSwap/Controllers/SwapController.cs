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

        public ActionResult Index()
        {
            return View();
        }
        //[Authorize]
        //public ActionResult Collection()
        //{
        //    string UserId = User.Identity.GetUserId();
        //    SwapViewModel swapViewModel = new SwapViewModel
        //    {
        //        UserCollections = db.UserCollections.Where(uc => uc.UserId == UserId).ToList()
        //    };

        //    return View(swapViewModel);
        //}


        [Authorize]
        public ActionResult UserCollection(int? id)
        {
            string UserId = User.Identity.GetUserId();
            UserCollection selectedCollection = db.UserCollections.Find(id);

            ViewBag.SelectedCollection = selectedCollection;
            ViewBag.Collections = db.UserCollections.Where(uc => uc.UserId == UserId).ToList();
            return View();
        }
    }
}