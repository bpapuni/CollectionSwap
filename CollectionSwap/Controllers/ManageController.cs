﻿using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using CollectionSwap.Models;
using Newtonsoft.Json;
using System.Collections.Generic;
using CollectionSwap.Helpers;
using System.Web.Services.Description;
using System.Data.Entity.Infrastructure;
using System.Reflection;
using System.Data.Entity;
using System.Collections.ObjectModel;

namespace CollectionSwap.Controllers
{
    [Authorize]
    public class ManageController : Controller
    {
        private ApplicationSignInManager _signInManager;
        private ApplicationUserManager _userManager;
        private ApplicationDbContext db = new ApplicationDbContext();

        public ManageController()
        {
        }

        public ManageController(ApplicationUserManager userManager, ApplicationSignInManager signInManager)
        {
            UserManager = userManager;
            SignInManager = signInManager;
        }

        public ApplicationSignInManager SignInManager
        {
            get
            {
                return _signInManager ?? HttpContext.GetOwinContext().Get<ApplicationSignInManager>();
            }
            private set 
            { 
                _signInManager = value; 
            }
        }

        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }

        //
        // GET: /Manage/Index
        public async Task<ActionResult> Index(ManageMessageId? message)
        {
            ViewBag.StatusMessage =
                message == ManageMessageId.ChangePasswordSuccess ? "Your password has been changed."
                : message == ManageMessageId.SetPasswordSuccess ? "Your password has been set."
                : message == ManageMessageId.SetTwoFactorSuccess ? "Your two-factor authentication provider has been set."
                : message == ManageMessageId.Error ? "An error has occurred."
                : message == ManageMessageId.AddPhoneSuccess ? "Your phone number was added."
                : message == ManageMessageId.RemovePhoneSuccess ? "Your phone number was removed."
                : "";

            var userId = User.Identity.GetUserId();
            var model = new IndexViewModel
            {
                HasPassword = HasPassword(),
                PhoneNumber = await UserManager.GetPhoneNumberAsync(userId),
                TwoFactor = await UserManager.GetTwoFactorEnabledAsync(userId),
                Logins = await UserManager.GetLoginsAsync(userId),
                BrowserRemembered = await AuthenticationManager.TwoFactorBrowserRememberedAsync(userId),
                Collections = db.Collections.ToList(),
                UserCollections = db.UserCollections.Where(uc => uc.UserId == userId && uc.Archived == false).ToList(),
                ChangeAddress = db.Addresses.Where(address => address.UserId == userId).FirstOrDefault()
            };

            return View(model);
        }

        //
        // GET: /Manage/FindSwaps

        [Authorize]
        public ActionResult FindSwapsPartial()
        {
            var userId = User.Identity.GetUserId();
            var model = new FindSwapsViewModel
            {
                Users = db.Users.ToList(),
                Collections = db.Collections.ToList(),
                UserCollections = db.UserCollections.Where(uc => uc.UserId == userId && uc.Archived == false).ToList(),
                UserSwaps = db.Swaps.Where(swap => swap.Receiver.Id == userId).ToList(),
                Feedbacks = db.Feedbacks.ToList()
            };

            var partial = Helper.RenderViewToString(ControllerContext, "_FindSwaps", model, true);
            return Json(new { PartialView = partial, RefreshTargets = new { first = "#find-swaps-container" } });
        }

        [Authorize]
        public ActionResult DisplaySwapMatches(int? id)
        {
            var userId = User.Identity.GetUserId();
            var selectedCollection = db.UserCollections.Find(id);
            var model = new FindSwapsViewModel
            {
                Users = db.Users.ToList(),
                Collections = db.Collections.ToList(),
                UserCollections = db.UserCollections.Where(uc => uc.UserId == userId && uc.Archived == false).ToList(),
                UserSwaps = db.Swaps.Where(swap => swap.Sender.Id == userId || swap.Receiver.Id == userId).ToList(),
                Feedbacks = db.Feedbacks.ToList()
            };

            if (id.HasValue && selectedCollection != null && selectedCollection.UserId == userId)
            {
                model.MatchingSwapViews = selectedCollection.FindMatchingSwaps(db);
                ViewBag.SelectedCollection = selectedCollection;
            }

            ViewBag.Status = TempData["Status"];
            var partial = Helper.RenderViewToString(ControllerContext, "_FindSwaps", model, true);
            return Json(new { PartialView = partial, RefreshTargets = new { first = "#find-swaps-container" } }, JsonRequestBehavior.AllowGet);
        }

        //
        // GET: /Manage/Account

        [Authorize]
        public ActionResult AccountPartial()
        {
            var userId = User.Identity.GetUserId();
            var model = new IndexViewModel
            {
                ChangeAddress = db.Addresses.Where(address => address.UserId == userId).ToList().LastOrDefault(),
            };

            var partial = Helper.RenderViewToString(ControllerContext, "_Account", model, true);
            return Json(new { PartialView = partial, RefreshTargets = new { first = "#account-container" } });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult ChangeEmail(IndexViewModel model)
        {
            var userId = User.Identity.GetUserId();
            var partial = String.Empty;
            model.ChangeAddress = db.Addresses.Where(address => address.UserId == userId).FirstOrDefault();

            if (!ModelState.IsValid)
            {
                partial = Helper.RenderViewToString(ControllerContext, "_Account", model, true);
                return Json(new { PartialView = partial, RefreshTargets = new { first = "#account-container" } });
            }

            
            var user = db.Users.Find(userId);
            var status = user.ChangeEmail(model.ChangeEmail.OldEmail, model.ChangeEmail.NewEmail, db);

            switch (status)
            {
                case "Incorrect email":
                    ModelState.AddModelError("ChangeEmail.OldEmail", status);
                    partial = Helper.RenderViewToString(ControllerContext, "_Account", model, true);
                    return Json(new { PartialView = partial, RefreshTargets = new { first = "#account-container" } });
                case "This email already exists":
                    ModelState.AddModelError("ChangeEmail.NewEmail", status);
                    partial = Helper.RenderViewToString(ControllerContext, "_Account", model, true);
                    return Json(new { PartialView = partial, RefreshTargets = new { first = "#account-container" } });
                default:
                    break;
            }

            ViewBag.Status = "Your email has been changed.";
            partial = Helper.RenderViewToString(ControllerContext, "_Account", model, true);
            return Json(new { PartialView = partial, RefreshTargets = new { first = "#account-container" }, FormResetTarget = "#change-email-form" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<ActionResult> ChangePassword(IndexViewModel model)
        {
            var userId = User.Identity.GetUserId();
            var partial = String.Empty;
            model.ChangeAddress = db.Addresses.Where(address => address.UserId == userId).FirstOrDefault();

            if (!ModelState.IsValid)
            {
                partial = Helper.RenderViewToString(ControllerContext, "_Account", model, true);
                return Json(new { PartialView = partial, RefreshTargets = new { first = "#account-container" } });
            }

            if (model.ChangePassword.NewPassword == model.ChangePassword.OldPassword)
            {
                ModelState.AddModelError("ChangePassword.NewPassword", "New password must be different from your current password.");
                partial = Helper.RenderViewToString(ControllerContext, "_Account", model, true);
                return Json(new { PartialView = partial, RefreshTargets = new { first = "#account-container" } });
            }

            var result = await UserManager.ChangePasswordAsync(User.Identity.GetUserId(), model.ChangePassword.OldPassword, model.ChangePassword.NewPassword);
            if (!result.Succeeded)
            {
                string[] errorMessages = result.Errors.First().Split(new[] { ". " }, StringSplitOptions.None);
                foreach (string message in errorMessages)
                {
                    switch (message)
                    {
                        case "Incorrect password.":
                            ModelState.AddModelError("ChangePassword.OldPassword", message);
                            break;
                        default:
                            ModelState.AddModelError("ChangePassword", message);
                            break;
                    } 
                    
                }


                partial = Helper.RenderViewToString(ControllerContext, "_Account", model, true);
                return Json(new { PartialView = partial, RefreshTargets = new { first = "#account-container" } });
            }

            var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
            if (user != null)
            {
                await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
            }

            ViewBag.Status = "Your password has been changed.";
            partial = Helper.RenderViewToString(ControllerContext, "_Account", model, true);
            return Json(new { PartialView = partial, RefreshTargets = new { first = "#account-container" }, FormResetTarget = "#change-password-form" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<ActionResult> ChangeAddress(IndexViewModel model)
        {
            var partial = String.Empty;
            if (!ModelState.IsValid)
            {
                partial = Helper.RenderViewToString(ControllerContext, "_Account", model, true);
                return Json(new { PartialView = partial, RefreshTargets = new { first = "#account-container" } });
            }

            var result = await model.ChangeAddress.CreateAddressAsync(User.Identity.GetUserId(), db);
            if (!result.Succeeded)
            {
                partial = Helper.RenderViewToString(ControllerContext, "_Account", model, true);
                return Json(new { PartialView = partial, RefreshTargets = new { first = "#account-container" } });
            }

            ViewBag.Status = "Your mailing address has been changed.";
            partial = Helper.RenderViewToString(ControllerContext, "_Account", model, true);
            return Json(new { PartialView = partial, RefreshTargets = new { first = "#account-container" } });
        }

        //
        // GET: /Manage/ManageCollections

        [Authorize(Roles = "Admin")]
        public ActionResult ManageCollectionsPartial(int? id)
        {
            var partial = String.Empty;
            var model = new ManageCollectionsViewModel
            {
                Collections = db.Collections.ToList(),
                CreateCollection = new CreateCollectionModel()
            };

            if (id.HasValue)
            {
                model.EditCollection = new EditCollectionModel { Collection = db.Collections.Find(id) };
            }

            ViewBag.ShouldDisplay = true;
            partial = Helper.RenderViewToString(ControllerContext, "_ManageCollections", model, true);
            return Json(new { PartialView = partial });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult CreateCollection(ManageCollectionsViewModel model)
        {
            var partial = String.Empty;
            if (!ModelState.IsValid)
            {
                model.Collections = db.Collections.ToList();
                partial = Helper.RenderViewToString(ControllerContext, "_ManageCollections", model, true);
                return Json(new { PartialView = partial, RefreshTargets = new { first = "#manage-collections-container" } });
            }

            var success = Collection.Create(model.CreateCollection, db);
            if (!success)
            {
                ModelState.AddModelError("CreateCollection.fileInput", "Selected zip file did not contain any images.");
            }
            model.Collections = db.Collections.ToList();

            partial = Helper.RenderViewToString(ControllerContext, "_ManageCollections", model, true);
            return Json(new { PartialView = partial, RefreshTargets = new { first = "#manage-collections-container" } });
        }

        //
        // GET: /Manage/ManageCollections/_EditCollection

        [Authorize(Roles = "Admin")]
        public ActionResult EditCollection(int id)
        {
            var partial = String.Empty;

            var mcModel = new ManageCollectionsViewModel
            {
                Collections = db.Collections.ToList(),
                EditCollection = new EditCollectionModel { Collection = db.Collections.Find(id) }
            };
            partial = Helper.RenderViewToString(ControllerContext, "_ManageCollections", mcModel, true);
            return Json(new { PartialView = partial, RefreshTargets = new { first = "#manage-collections-container", second = "#edit-collection-container" }, ScrollTarget = "#edit-collection-container" }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult DeleteCollection(int? collectionId)
        {
            var partial = String.Empty;
            Collection collection = db.Collections.Find(collectionId);
            collection.Delete(db);

            var model = new ManageCollectionsViewModel
            {
                Collections = db.Collections.ToList(),
                CreateCollection = new CreateCollectionModel()
            };

            ViewBag.ShouldDisplay = true;
            partial = Helper.RenderViewToString(ControllerContext, "_ManageCollections", model, true);
            return Json(new { PartialView = partial, RefreshTargets = new { first = "#manage-collections-container" } });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult ChangeCollectionName([Bind(Prefix = "Collection")] Collection model)
        {
            var partial = String.Empty;
            var mcViewModel = new ManageCollectionsViewModel
            {
                Collections = db.Collections.ToList(),
                CreateCollection = new CreateCollectionModel(),
                EditCollection = new EditCollectionModel { Collection = db.Collections.Find(model.Id) }
            };

            if (!ModelState.IsValid)
            {
                partial = Helper.RenderViewToString(ControllerContext, "_ManageCollections", mcViewModel, true);
                return Json(new { PartialView = partial, RefreshTargets = new { first = "#edit-collection-container" } });

            }

            Collection collection = db.Collections.Find(model.Id);
            collection.Update(model.Name, db);
            mcViewModel.EditCollection.Collection = collection;

            ViewBag.Status = "Collection name updated successfully";
            partial = Helper.RenderViewToString(ControllerContext, "_ManageCollections", mcViewModel, true);
            return Json(new { PartialView = partial, RefreshTargets = new { first = "#manage-collections-container", second = "#edit-collection-container" } });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult AddItem(EditCollectionModel model)
        {
            var partial = String.Empty;
            var collection = db.Collections.Find(model.Collection.Id);
            var mcViewModel = new ManageCollectionsViewModel();

            if (!ModelState.IsValidField("FileInput"))
            {
                model.Collection = collection;
                mcViewModel.Collections = db.Collections.ToList();
                mcViewModel.EditCollection = model;

                partial = Helper.RenderViewToString(ControllerContext, "_ManageCollections", mcViewModel, true);
                return Json(new { PartialView = partial, RefreshTargets = new { first = "#edit-collection-container" } });
            }

            collection.AddItem(model.FileInput, db);
            model.Collection = collection;
            mcViewModel.Collections = db.Collections.ToList();
            mcViewModel.EditCollection = model;

            ViewBag.Status = "Item successfully added to collection";
            partial = Helper.RenderViewToString(ControllerContext, "_ManageCollections", mcViewModel, true);
            return Json(new { PartialView = partial, RefreshTargets = new { first = "#manage-collections-container", second = "#edit-collection-container" } });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult EditItem(int collectionId, int itemId, string fileName, HttpPostedFileBase fileInput)
        {
            var collection = db.Collections.Find(collectionId);
            var editCollection = new EditCollectionModel { Collection = collection };
            var partial = String.Empty;
            if (!ModelState.IsValid)
            {
                partial = Helper.RenderViewToString(ControllerContext, "_EditCollection", editCollection, true);
                return Json(new { PartialView = partial, RefreshTargets = new { first = "#edit-collection-container" } });

            }

            collection.EditItem(itemId, fileName, fileInput, db);
            var model = new ManageCollectionsViewModel
            {
                Collections = db.Collections.ToList(),
                CreateCollection = new CreateCollectionModel(),
                EditCollection = new EditCollectionModel { Collection = collection }
            };

            ViewBag.ShouldDisplay = true;
            partial = Helper.RenderViewToString(ControllerContext, "_ManageCollections", model, true);
            return Json(new { PartialView = partial, RefreshTargets = new { first = "#edit-collection-container" } });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult DeleteItem(int? collectionId, string fileName)
        {
            var collection = db.Collections.Find(collectionId);
            collection.DeleteItem(fileName, db);

            var model = new ManageCollectionsViewModel
            {
                Collections = db.Collections.ToList(),
                CreateCollection = new CreateCollectionModel(),
                EditCollection = new EditCollectionModel { Collection = collection }
            };

            ViewBag.ShouldDisplay = true;
            ViewBag.EditCollectionStatus = "Item successfully removed from collection.";
            var partial = Helper.RenderViewToString(ControllerContext, "_ManageCollections", model, true);
            return Json(new { PartialView = partial, RefreshTargets = new { first = "#manage-collections-container", second = "#edit-collection-container" } });
        }

        //
        // GET: /Manage/YourCollections

        [Authorize]
        public ActionResult YourCollectionsPartial()
        {
            var userId = User.Identity.GetUserId();
            var partial = String.Empty;
            var model = new YourCollectionViewModel
            {
                Collections = db.Collections.ToList(),
                UserCollections = db.UserCollections.Where(uc => uc.UserId == userId && uc.Archived == false).ToList(),
            };

            partial = Helper.RenderViewToString(ControllerContext, "_YourCollections", model, true);
            return Json(new { PartialView = partial });
        }

        //
        // GET: /Manage/YourCollections

        [Authorize]
        public ActionResult UserCollection(int? id)
        {
            var partial = String.Empty;
            var userId = User.Identity.GetUserId();
            var ycModel = new YourCollectionViewModel
            {
                Collections = db.Collections.ToList(),
                UserCollections = db.UserCollections.Where(uc => uc.UserId == userId && uc.Archived == false).ToList(),
            };

            if (!id.HasValue)
            {
                partial = Helper.RenderViewToString(ControllerContext, "_YourCollections", ycModel, true);
                return Json(new { PartialView = partial, RefreshTargets = new { first = "#your-collections-container" } }, JsonRequestBehavior.AllowGet);
            }
            var userCollection = db.UserCollections.Find(id);
            var ucModel = new UserCollectionModel
            {
                Collection = db.Collections.Find(userCollection.CollectionId),
                UserCollection = userCollection
            };

            ycModel.EditCollection = ucModel;
            partial = Helper.RenderViewToString(ControllerContext, "_YourCollections", ycModel, true);
            return Json(new { PartialView = partial, RefreshTargets = new { first = "#your-collections-container", /*second = "#create-user-collection-container",*/ third = "#user-collection-container" }, ScrollTarget = "#user-collection-container" }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult ChangeUserCollectionName([Bind(Prefix = "UserCollection")] UserCollection model)
        {
            var partial = String.Empty;
            var userCollection = db.UserCollections.Find(model.Id);
            var ucModel = new UserCollectionModel
            {
                Collection = db.Collections.Find(userCollection.CollectionId),
                UserCollection = userCollection
            };
            var ycViewModel = new YourCollectionViewModel
            {
                Collections = db.Collections.ToList()
            };

            if (!ModelState.IsValid)
            {
                ycViewModel.UserCollections = db.UserCollections.Where(uc => uc.UserId == model.UserId).ToList();
                ycViewModel.EditCollection = ucModel;

                partial = Helper.RenderViewToString(ControllerContext, "_YourCollections", ycViewModel, true);
                return Json(new { PartialView = partial, RefreshTargets = new { first = "#user-collection-container" } });
            }

            userCollection.Update("Name", model.UserId, model.Name, db);
            ucModel.UserCollection = userCollection;

            ycViewModel.UserCollections = db.UserCollections.Where(uc => uc.UserId == model.UserId).ToList();
            ycViewModel.EditCollection = ucModel;

            ViewBag.Status = "Collection name updated successfully";
            partial = Helper.RenderViewToString(ControllerContext, "_YourCollections", ycViewModel, true);
            return Json(new { PartialView = partial, RefreshTargets = new { first = "#your-collections-container", second = "#user-collection-container" } });
        }

        [HttpPost]
        [Authorize]
        public void ToggleCharityCollection(int id)
        {
            var userId = User.Identity.GetUserId();
            var userCollection = db.UserCollections.Find(id);
            userCollection.Update("Charity", userId, null, db);
        }

        [HttpPost]
        [Authorize]
        public ActionResult CreateUserCollection(int id)
        {
            var partial = String.Empty;

            var userId = User.Identity.GetUserId();
            var newUserCollection = Models.UserCollection.Create(id, userId, db);

            return RedirectToAction("UserCollection", new { id = newUserCollection.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<ActionResult> DeleteUserCollection(int id)
        {
            var partial = String.Empty;
            var userCollection = db.UserCollections.Find(id);
            await userCollection.Delete(db);

            var userId = User.Identity.GetUserId();
            var ycViewModel = new YourCollectionViewModel
            {
                Collections = db.Collections.ToList(),
                UserCollections = db.UserCollections.Where(uc => uc.UserId == userId && uc.Archived == false).ToList(),
            };

            partial = Helper.RenderViewToString(ControllerContext, "_YourCollections", ycViewModel, true);
            return Json(new { PartialView = partial, RefreshTargets = new { first = "#your-collections-container" } });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult UpdateItemCount(int quantity, int index, string userId, int ucId)
        {
            var partial = String.Empty;
            UserCollection userCollection = db.UserCollections.Find(ucId);
            userCollection.Update("Quantity", userId, JsonConvert.SerializeObject(new { index, quantity }), db);

            var ycViewModel = new YourCollectionViewModel
            {
                Collections = db.Collections.ToList(),
                UserCollections = db.UserCollections.Where(uc => uc.UserId == userId && uc.Archived == false).ToList(),
            };

            partial = Helper.RenderViewToString(ControllerContext, "_YourCollections", ycViewModel, true);
            return Json(new { PartialView = partial, RefreshTargets = new { first = "#your-collections-container" } });
            //return null;
        }

        //
        // GET: /Manage/SwapHistory

        [Authorize]
        public ActionResult SwapHistoryPartial(int? id)
        {
            var partial = String.Empty;
            var userId = User.Identity.GetUserId();
            var swaps = db.Swaps.Where(swap => swap.SenderId == userId || swap.ReceiverId == userId).ToList();

            var shModel = new SwapHistoryViewModel
            {
                Swaps = ProcessCharityRequests(swaps),
                Offer = null
            };

            if (id.HasValue)
            {
                return Offer(id.Value);
            }

            ViewBag.Status = TempData["Status"];
            partial = Helper.RenderViewToString(ControllerContext, "_SwapHistory", shModel, true);
            return Json(new { PartialView = partial, RefreshTargets = new { first = ".scroll-snap-row" } }, JsonRequestBehavior.AllowGet);
        }

        private List<Swap> ProcessCharityRequests(List<Swap> swaps)
        {
            var userId = User.Identity.GetUserId();

            // Returns all swaps for receivers, or all swaps excluding charity for senders
            var processedSwaps = swaps
                .Where(s => (s.Sender.Id == userId && s.Status.Contains("charity") == true) == false)
                .ToList();

            var charitySwaps = swaps
                .Where(s => s.Sender.Id == userId && s.Status.Contains("charity") == true)
                .ToList();

            // Creates a list of all user collections marked as charity
            var charityCollectionsIds = charitySwaps
                .Select(s => s.SenderCollection.Id)
                .Distinct()
                .ToList();

            foreach (var id in charityCollectionsIds)
            {
                // Create a List<int> of items being given away
                var charityItems = JsonConvert.DeserializeObject<List<int>>(swaps.Select(s => s.SenderCollection).Where(s => s.Id == id).FirstOrDefault().ItemCountJSON)
                    .Select((value, index) => new { Value = value, Index = index })
                    .Where(item => item.Value != 0)
                    .Select(item => item.Index)
                    .ToList();


                var requestedSwaps = charitySwaps.Where(s => s.SenderCollection.Id == id).ToList();

                // Creates a List<List<int>> (list of lists) of all receivers item counts
                var receiversItemCountAndRequestDate = requestedSwaps
                    .Select(s => new { Id = s.Id, ItemCount = JsonConvert.DeserializeObject<List<int>>(s.ReceiverCollection.ItemCountJSON), Date = s.StartDate })
                    .ToList();

                // Create List<List<int>> (list of lists) of the items each receiver is missing that are available in the charity items
                var missingItems = receiversItemCountAndRequestDate
                    .Select(innerList => new
                    {
                        id = innerList.Id,
                        Items = innerList.ItemCount
                        .Select((value, index) => new { Value = value, Index = index })
                        .Where(item => item.Value == 0 && charityItems.Contains(item.Index))
                        .Select(item => item.Index)
                        .ToList(),
                        Date = innerList.Date
                    })
                    .OrderByDescending(obj => obj.Items.Count())
                    .ThenBy(obj => obj.Date)
                    .FirstOrDefault();

                processedSwaps.Add(swaps.Where(s => s.Id == missingItems.id).FirstOrDefault());
            }

            return processedSwaps;
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult> ConfirmSentReceived(int id, string type)
        {
            var userId = User.Identity.GetUserId();
            var swap = db.Swaps.Find(id);
            await swap.Confirm(type, userId, db);

            var swaps = db.Swaps.Where(s => s.SenderId == userId || s.ReceiverId == userId).ToList();

            var shModel = new SwapHistoryViewModel
            {
                Swaps = ProcessCharityRequests(swaps),
                //Feedback = null,
                Offer = null
            };

            var partial = Helper.RenderViewToString(ControllerContext, "_SwapHistory", shModel, true);
            return Json(new { PartialView = partial, RefreshTargets = new { first = ".scroll-snap-row" } });
        }

        //
        // GET: /Manage/SwapHistory

        [Authorize]
        public ActionResult Feedback(int id)
        {
            var partial = String.Empty;
            var userId = User.Identity.GetUserId();

            var swaps = db.Swaps.Where(swap => swap.SenderId == userId || swap.ReceiverId == userId).ToList();

            var fbModel = new FeedbackViewModel
            {
                Swap = db.Swaps.Find(id),
                Feedback = db.Feedbacks.Where(fb => fb.SenderId == userId && fb.SwapId == id).FirstOrDefault()
            };

            var shModel = new SwapHistoryViewModel
            {
                Swaps = ProcessCharityRequests(swaps),
                //Feedback = fbModel,
                Offer = null
            };

            partial = Helper.RenderViewToString(ControllerContext, "_SwapHistory", shModel, true);
            return Json(new { PartialView = partial, RefreshTargets = new { first = "#history-container", second = "#feedback-container" }, ScrollTarget = "#feedback-container" }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [Authorize]
        public ActionResult PlaceFeedback([Bind(Prefix = "Feedback")] Feedback model)
        {
            var partial = String.Empty;
            var userId = User.Identity.GetUserId();

            var swaps = db.Swaps.Where(swap => swap.SenderId == userId || swap.ReceiverId == userId).ToList();

            var fbModel = new FeedbackViewModel
            {
                Swap = db.Swaps.Find(model.SwapId),
                Feedback = null
            };

            var shModel = new SwapHistoryViewModel
            {
                Swaps = ProcessCharityRequests(swaps),
                //Feedback = fbModel,
                Offer = null
            };

            if (!ModelState.IsValid)
            {
                partial = Helper.RenderViewToString(ControllerContext, "_SwapHistory", shModel, true);
                return Json(new { PartialView = partial, RefreshTargets = new { first = "#feedback-container" } });
            }

            var feedback = model.Create(userId, db);
            fbModel.Feedback = feedback;

            shModel.Swaps = db.Swaps.Where(s => s.SenderId == userId || s.ReceiverId == userId).ToList();
            //shModel.Feedback = null;

            ViewBag.Status = "Thank you for your feedback";
            partial = Helper.RenderViewToString(ControllerContext, "_SwapHistory", shModel, true);
            return Json(new { PartialView = partial, RefreshTargets = new { first = ".scroll-snap-row" } });
        }

        //
        // GET: /Manage/YourSwaps/Offer

        [Authorize]
        public ActionResult Offer(int id)
        {
            var partial = String.Empty;
            var userId = User.Identity.GetUserId();

            // Get all swaps involving the user to update their swap history
            var usersSwaps = db.Swaps.Where(s => s.SenderId == userId || s.ReceiverId == userId).ToList();

            // Get the offered swap
            var swap = usersSwaps.Where(s => s.Id == id).FirstOrDefault();

            var offerModel = new SwapViewModel
            {
                Swap = swap,
                Feedback = db.Feedbacks.Where(fb => fb.SenderId == userId && fb.SwapId == id).FirstOrDefault(),
                Address = db.Addresses.Where(a => a.UserId != userId && (a.UserId == swap.SenderId || a.UserId == swap.ReceiverId)).FirstOrDefault(),
                Validation = swap.Validate(userId, db)
            };

            var shModel = new SwapHistoryViewModel
            {
                Swaps = ProcessCharityRequests(usersSwaps),
                Offer = offerModel
            };

            partial = Helper.RenderViewToString(ControllerContext, "_SwapHistory", shModel, true);
            return Json(new { PartialView = partial, RefreshTargets = new { first = "#history-container", second = "#offer-container" }, ScrollTarget = "#offer-container" }, JsonRequestBehavior.AllowGet);
        }

        //
        // GET: /Manage/SwapHistory

        [Authorize]
        public ActionResult Instructions(int id)
        {
            var partial = String.Empty;
            var userId = User.Identity.GetUserId();
            var swaps = db.Swaps.Where(swap => swap.SenderId == userId || swap.ReceiverId == userId).ToList();

            var fbModel = new FeedbackViewModel
            {
                Swap = db.Swaps.Find(id),
                Feedback = db.Feedbacks.Where(fb => fb.SenderId == userId && fb.SwapId == id).FirstOrDefault()
            };

            var shModel = new SwapHistoryViewModel
            {
                Swaps = ProcessCharityRequests(swaps),
                //Feedback = fbModel,
                Offer = null
            };

            partial = Helper.RenderViewToString(ControllerContext, "_SwapHistory", shModel, true);
            return Json(new { PartialView = partial, RefreshTargets = new { first = "#history-container", second = "#feedback-container" }, ScrollTarget = "#feedback-container" }, JsonRequestBehavior.AllowGet);
        }



        //
        // POST: /Manage/RemoveLogin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RemoveLogin(string loginProvider, string providerKey)
        {
            ManageMessageId? message;
            var result = await UserManager.RemoveLoginAsync(User.Identity.GetUserId(), new UserLoginInfo(loginProvider, providerKey));
            if (result.Succeeded)
            {
                var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
                if (user != null)
                {
                    await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                }
                message = ManageMessageId.RemoveLoginSuccess;
            }
            else
            {
                message = ManageMessageId.Error;
            }
            return RedirectToAction("ManageLogins", new { Message = message });
        }

        //
        // GET: /Manage/AddPhoneNumber
        public ActionResult AddPhoneNumber()
        {
            return View();
        }

        //
        // POST: /Manage/AddPhoneNumber
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> AddPhoneNumber(AddPhoneNumberViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            // Generate the token and send it
            var code = await UserManager.GenerateChangePhoneNumberTokenAsync(User.Identity.GetUserId(), model.Number);
            if (UserManager.SmsService != null)
            {
                var message = new IdentityMessage
                {
                    Destination = model.Number,
                    Body = "Your security code is: " + code
                };
                await UserManager.SmsService.SendAsync(message);
            }
            return RedirectToAction("VerifyPhoneNumber", new { PhoneNumber = model.Number });
        }

        //
        // POST: /Manage/EnableTwoFactorAuthentication
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EnableTwoFactorAuthentication()
        {
            await UserManager.SetTwoFactorEnabledAsync(User.Identity.GetUserId(), true);
            var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
            if (user != null)
            {
                await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
            }
            return RedirectToAction("Index", "Manage");
        }

        //
        // POST: /Manage/DisableTwoFactorAuthentication
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DisableTwoFactorAuthentication()
        {
            await UserManager.SetTwoFactorEnabledAsync(User.Identity.GetUserId(), false);
            var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
            if (user != null)
            {
                await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
            }
            return RedirectToAction("Index", "Manage");
        }

        //
        // GET: /Manage/VerifyPhoneNumber
        public async Task<ActionResult> VerifyPhoneNumber(string phoneNumber)
        {
            var code = await UserManager.GenerateChangePhoneNumberTokenAsync(User.Identity.GetUserId(), phoneNumber);
            // Send an SMS through the SMS provider to verify the phone number
            return phoneNumber == null ? View("Error") : View(new VerifyPhoneNumberViewModel { PhoneNumber = phoneNumber });
        }

        //
        // POST: /Manage/VerifyPhoneNumber
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> VerifyPhoneNumber(VerifyPhoneNumberViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var result = await UserManager.ChangePhoneNumberAsync(User.Identity.GetUserId(), model.PhoneNumber, model.Code);
            if (result.Succeeded)
            {
                var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
                if (user != null)
                {
                    await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                }
                return RedirectToAction("Index", new { Message = ManageMessageId.AddPhoneSuccess });
            }
            // If we got this far, something failed, redisplay form
            ModelState.AddModelError("", "Failed to verify phone");
            return View(model);
        }

        //
        // POST: /Manage/RemovePhoneNumber
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RemovePhoneNumber()
        {
            var result = await UserManager.SetPhoneNumberAsync(User.Identity.GetUserId(), null);
            if (!result.Succeeded)
            {
                return RedirectToAction("Index", new { Message = ManageMessageId.Error });
            }
            var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
            if (user != null)
            {
                await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
            }
            return RedirectToAction("Index", new { Message = ManageMessageId.RemovePhoneSuccess });
        }

        //
        // GET: /Manage/SetPassword
        public ActionResult SetPassword()
        {
            return View();
        }

        //
        // POST: /Manage/SetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SetPassword(SetPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await UserManager.AddPasswordAsync(User.Identity.GetUserId(), model.NewPassword);
                if (result.Succeeded)
                {
                    var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
                    if (user != null)
                    {
                        await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                    }
                    return RedirectToAction("Index", new { Message = ManageMessageId.SetPasswordSuccess });
                }
                AddErrors(result);
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Manage/ManageLogins
        public async Task<ActionResult> ManageLogins(ManageMessageId? message)
        {
            ViewBag.StatusMessage =
                message == ManageMessageId.RemoveLoginSuccess ? "The external login was removed."
                : message == ManageMessageId.Error ? "An error has occurred."
                : "";
            var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
            if (user == null)
            {
                return View("Error");
            }
            var userLogins = await UserManager.GetLoginsAsync(User.Identity.GetUserId());
            var otherLogins = AuthenticationManager.GetExternalAuthenticationTypes().Where(auth => userLogins.All(ul => auth.AuthenticationType != ul.LoginProvider)).ToList();
            ViewBag.ShowRemoveButton = user.PasswordHash != null || userLogins.Count > 1;
            return View(new ManageLoginsViewModel
            {
                CurrentLogins = userLogins,
                OtherLogins = otherLogins
            });
        }

        //
        // POST: /Manage/LinkLogin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LinkLogin(string provider)
        {
            // Request a redirect to the external login provider to link a login for the current user
            return new AccountController.ChallengeResult(provider, Url.Action("LinkLoginCallback", "Manage"), User.Identity.GetUserId());
        }

        //
        // GET: /Manage/LinkLoginCallback
        public async Task<ActionResult> LinkLoginCallback()
        {
            var loginInfo = await AuthenticationManager.GetExternalLoginInfoAsync(XsrfKey, User.Identity.GetUserId());
            if (loginInfo == null)
            {
                return RedirectToAction("ManageLogins", new { Message = ManageMessageId.Error });
            }
            var result = await UserManager.AddLoginAsync(User.Identity.GetUserId(), loginInfo.Login);
            return result.Succeeded ? RedirectToAction("ManageLogins") : RedirectToAction("ManageLogins", new { Message = ManageMessageId.Error });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _userManager != null)
            {
                _userManager.Dispose();
                _userManager = null;
            }

            base.Dispose(disposing);
        }

        #region Helpers
        // Used for XSRF protection when adding external logins
        private const string XsrfKey = "XsrfId";

        private IAuthenticationManager AuthenticationManager
        {
            get
            {
                return HttpContext.GetOwinContext().Authentication;
            }
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }

        private bool HasPassword()
        {
            var user = UserManager.FindById(User.Identity.GetUserId());
            if (user != null)
            {
                return user.PasswordHash != null;
            }
            return false;
        }

        private bool HasPhoneNumber()
        {
            var user = UserManager.FindById(User.Identity.GetUserId());
            if (user != null)
            {
                return user.PhoneNumber != null;
            }
            return false;
        }

        public enum ManageMessageId
        {
            AddPhoneSuccess,
            ChangePasswordSuccess,
            SetTwoFactorSuccess,
            SetPasswordSuccess,
            RemoveLoginSuccess,
            RemovePhoneSuccess,
            Error
        }

#endregion
    }
}