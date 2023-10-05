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
using System.Reflection;
using System.Threading.Tasks;
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
                //Users = db.Users.ToList(),
                Collections = db.Collections.ToList(),
                UserCollections = db.UserCollections.Where(uc => uc.User.Id == userId).ToList(),
                UserSwaps = db.Swaps.Where(swap => swap.Receiver.Id == userId).ToList(),
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
                Collections = db.Collections.ToList(),
                UserCollections = db.UserCollections.Where(uc => uc.User.Id == userId).ToList(),
                UserSwaps = db.Swaps.Where(swap => swap.Receiver.Id == userId).ToList(),
                Feedbacks = db.Feedbacks.ToList()
            };

            var partial = Helper.RenderViewToString(ControllerContext, "~/Views/Manage/_FindSwaps.cshtml", model, true);
            return Json(new { PartialView = partial, RefreshTargets = new { first = "#find-swaps-container" } }, JsonRequestBehavior.AllowGet);
        }

        [Authorize]
        [HttpPost]
        public ActionResult ProcessSwap(SwapRequestViewModel request)
        {
            var userId = User.Identity.GetUserId();
            var swap = db.Swaps.Find(request.SwapId) == null ? new Swap() : db.Swaps.Find(request.SwapId);

            var result = swap.ProcessSwap(userId, request, db);
            if (!result.Succeeded)
            {
                return Json(new { reloadPage = false });
            }

            switch (result.SuccessType)
            {
                case "charity-requested":
                    TempData["Status"] = "You've requested these items";
                    return RedirectToAction("DisplaySwapMatches", "Manage", new { id = request.ReceiverUserCollectionId });
                case "charity-confirmed":
                    TempData["Status"] = "You've confirmed this request";
                    return RedirectToAction("SwapsPartial", "Manage");
                case "charity-canceled":
                    TempData["Status"] = "You've canceled this request";
                    return RedirectToAction("SwapsPartial", "Manage");
                case "requested":
                    TempData["Status"] = "Your swap request has been sent";
                    return RedirectToAction("DisplaySwapMatches", "Manage", new { id = request.SenderUserCollectionId });
                case "accepted":
                    TempData["Status"] = "You've accepted this swap";
                    return RedirectToAction("SwapsPartial", "Manage");
                case "confirmed":
                    TempData["Status"] = "You've confirmed this swap";
                    return RedirectToAction("SwapsPartial", "Manage");
                case "canceled":
                    TempData["Status"] = "You've canceled this swap";
                    return RedirectToAction("SwapsPartial", "Manage");
                case "declined":
                    TempData["Status"] = "You've declined this swap";
                    return RedirectToAction("SwapsPartial", "Manage");
                default:
                    return Json(new { reloadPage = false });
            }
        }
    }
}