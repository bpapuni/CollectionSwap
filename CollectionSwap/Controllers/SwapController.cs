using CollectionSwap.Helpers;
using CollectionSwap.Models;
using Microsoft.AspNet.Identity;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
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
            var userId = User.Identity.GetUserId();
            var model = new FindSwapsViewModel
            {
                Users = db.Users.ToList(),
                Collections = db.Collections.ToList(),
                UserCollections = db.UserCollections.Where(uc => uc.User.Id == userId).ToList(),
                OfferedSwaps = db.Swaps.Where(swap => swap.Receiver.Id == userId && swap.Status == "offered").ToList(),
                AcceptedSwaps = db.Swaps.Where(swap => swap.Sender.Id == userId && swap.Status == "accepted").ToList(),
                ConfirmedSwaps = db.Swaps.Where(swap => swap.Sender.Id == userId && swap.Status == "confirmed").ToList(),
                Feedbacks = db.Feedbacks.ToList()
            };

            return View(model);
        }

        [Authorize]
        public ActionResult UserCollection(int? id)
        {
            var userId = User.Identity.GetUserId();
            var selectedCollection = db.UserCollections.Find(id);
            var model = new FindSwapsViewModel
            {
                Users = db.Users.ToList(),
                Collections = db.Collections.ToList(),
                UserCollections = db.UserCollections.Where(uc => uc.User.Id == userId).ToList(),
                OfferedSwaps = db.Swaps.Where(swap => swap.Receiver.Id == userId && swap.Status == "offered").ToList(),
                AcceptedSwaps = db.Swaps.Where(swap => swap.Sender.Id == userId && swap.Status == "accepted").ToList(),
                ConfirmedSwaps = db.Swaps.Where(swap => swap.Sender.Id == userId && swap.Status == "confirmed").ToList(),
                Feedbacks = db.Feedbacks.ToList()
            };

            if (id.HasValue && selectedCollection != null && selectedCollection.UserId == userId)
            {
                ViewBag.MatchingSwaps = selectedCollection.FindMatchingSwaps(db);
                ViewBag.SelectedCollection = selectedCollection;
            }

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
            var userId = User.Identity.GetUserId();
            var model = new FindSwapsViewModel
            {
                Users = db.Users.ToList(),
                Collections = db.Collections.ToList(),
                UserCollections = db.UserCollections.Where(uc => uc.User.Id == userId).ToList(),
                OfferedSwaps = db.Swaps.Where(swap => swap.Receiver.Id == userId && swap.Status == "offered").ToList(),
                AcceptedSwaps = db.Swaps.Where(swap => swap.Sender.Id == userId && swap.Status == "accepted").ToList(),
                ConfirmedSwaps = db.Swaps.Where(swap => swap.Sender.Id == userId && swap.Status == "confirmed").ToList(),
                Feedbacks = db.Feedbacks.ToList()
            };

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