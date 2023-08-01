using CollectionSwap.Models;
using Microsoft.AspNet.Identity;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.UI.WebControls;

namespace CollectionSwap.Controllers
{
    public class SwapController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        public ActionResult Index()
        {
            var currentUserId = User.Identity.GetUserId();
            var model = FindSwapsViewModel.Create(currentUserId, db);

            return View(model);
        }

        [Authorize]
        public ActionResult UserCollection(int? id)
        {
            var currentUserId = User.Identity.GetUserId();
            var model = FindSwapsViewModel.Create(currentUserId, db);
            UserCollection selectedCollection = db.UserCollections.Find(id);

            if (id == null)
            {
                ViewBag.SelectedCollection = selectedCollection;
                return View(model);
            }

            ViewBag.MatchingSwaps = selectedCollection.FindMatchingSwaps(db);
            ViewBag.SelectedCollection = selectedCollection;

            return View(model);
        }

        [Authorize]
        [HttpPost]
        public ActionResult ProcessSwap(Swap swap)
        {
            if (ModelState.IsValid)
            {
                Response.Cookies["swapSuccessMessage"].Value = swap.Process(db);
                return Json(new { reloadPage = true });
            }

            return Json(new { reloadPage = false });
        }

        [Authorize]
        public ActionResult Offers()
        {
            var currentUserId = User.Identity.GetUserId();
            var model = FindSwapsViewModel.Create(currentUserId, db);

            return View(model);
        }

        [Authorize]
        public ActionResult History()
        {
            var currentUserId = User.Identity.GetUserId();
            ViewBag.UserSwaps = db.Swaps
                                    .Include(s => s.Sender) // Eagerly load the Sender property
                                    .Include(s => s.Receiver) // Eagerly load the Receiver property
                                    .Include(s => s.Collection) // Eagerly load the Collection property
                                    .Where(swap => swap.Sender.Id == currentUserId || swap.Receiver.Id == currentUserId)
                                    .ToList();

            return View();
        }
    }
}