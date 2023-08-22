using System;
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
                UserCollections = db.UserCollections.Where(uc => uc.UserId == userId).ToList(),
                ChangeAddress = db.Addresses.Where(address => address.UserId == userId).FirstOrDefault()
            };

            return View(model);
        }

        //
        // GET: /Manage/Account

        [Authorize]
        public ActionResult AccountPartial()
        {
            var userId = User.Identity.GetUserId();
            var model = new IndexViewModel
            {
                ChangeAddress = db.Addresses.Where(address => address.UserId == userId).FirstOrDefault()
            };

            var partial = Helper.RenderViewToString(ControllerContext, "_Account", model, true);
            //var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);

            return Json(new { PartialView = partial, RefreshTargets = new { first = "#account-container" } });
            //return PartialView("_Account", model);
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
                    return Json(new { PartialView = partial });
                case "This email already exists":
                    ModelState.AddModelError("ChangeEmail.NewEmail", status);
                    partial = Helper.RenderViewToString(ControllerContext, "_Account", model, true);
                    return Json(new { PartialView = partial, RefreshTargets = new { first = "#account-container" } });
                default:
                    break;
            }

            ViewBag.ChangeEmailStatus = "Your email has been changed.";
            partial = Helper.RenderViewToString(ControllerContext, "_Account", model, true);
            return Json(new { PartialView = partial, RefreshTargets = new { first = "#account-container" } });
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

            ViewBag.ChangePasswordStatus = "Your password has been changed.";
            partial = Helper.RenderViewToString(ControllerContext, "_Account", model, true);
            return Json(new { PartialView = partial, RefreshTargets = new { first = "#account-container" } });
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
                //ModelState.AddModelError("ChangeAddress", result.Error);
                partial = Helper.RenderViewToString(ControllerContext, "_Account", model, true);
                return Json(new { PartialView = partial, RefreshTargets = new { first = "#account-container" } });
            }

            ViewBag.ChangeAddressStatus = "Your mailing address has been changed.";
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
                ViewBag.ShouldDisplay = true;
                partial = Helper.RenderViewToString(ControllerContext, "_ManageCollections", model, true);
                return Json(new { PartialView = partial, RefreshTargets = new { first = "#manage-collections-container" } });
                //return Json(new { PartialView = partial });
            }

            Collection.Create(model.CreateCollection, db);
            model.Collections = db.Collections.ToList();

            ViewBag.ShouldDisplay = true;
            partial = Helper.RenderViewToString(ControllerContext, "_ManageCollections", model, true);
            return Json(new { PartialView = partial, RefreshTargets = new { first = "#manage-collections-container" } });
            //return Json(new { PartialView = partial });
        }

        //
        // GET: /Manage/ManageCollections/_EditCollection

        [Authorize(Roles = "Admin")]
        public ActionResult EditCollection(int id)
        {
            var partial = String.Empty;
            var model = new EditCollectionModel { Collection = db.Collections.Find(id) };

            ViewBag.ShouldDisplay = true;
            partial = Helper.RenderViewToString(ControllerContext, "_EditCollection", model, true);
            return Json(new { PartialView = partial, RefreshTargets = new { first = "#edit-collection-container" }, ScrollTarget = "#edit-collection-container" }, JsonRequestBehavior.AllowGet);
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
            if (!ModelState.IsValid)
            {
                model.ItemListJSON = db.Collections.Find(model.Id).ItemListJSON;
                ViewBag.ShouldDisplay = true;
                partial = Helper.RenderViewToString(ControllerContext, "_EditCollection", model, true);
                return Json(new { PartialView = partial });

            }

            Collection collection = db.Collections.Find(model.Id);
            collection.Update(model.Name, db);

            var mcViewModel = new ManageCollectionsViewModel
            {
                Collections = db.Collections.ToList(),
                CreateCollection = new CreateCollectionModel(),
                EditCollection = new EditCollectionModel { Collection = db.Collections.Find(model.Id) }
            };

            ViewBag.ShouldDisplay = true;
            partial = Helper.RenderViewToString(ControllerContext, "_ManageCollections", mcViewModel, true);
            return Json(new { PartialView = partial, RefreshTargets = new { first = "#manage-collections-container" } });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult AddItem(EditCollectionModel model)
        {
            var partial = String.Empty;
            var collection = db.Collections.Find(model.Collection.Id);

            if (!ModelState.IsValidField("FileInput"))
            {
                model.Collection = collection;
                ViewBag.ShouldDisplay = true;
                partial = Helper.RenderViewToString(ControllerContext, "_EditCollection", model, true);
                return Json(new { PartialView = partial, RefreshTargets = new { first = "#edit-collection-container" } });

            }

            collection.AddItem(model.FileInput, db);
            model.Collection = collection;

            ViewBag.ShouldDisplay = true;
            ViewBag.EditCollectionStatus = "Item successfully added to collection.";
            partial = Helper.RenderViewToString(ControllerContext, "_EditCollection", model, true);
            return Json(new { PartialView = partial, RefreshTargets = new { first = "#edit-collection-container" } });
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
                ViewBag.ShouldDisplay = true;
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
                UserCollections = db.UserCollections.Where(uc => uc.UserId == userId).ToList(),
            };

            partial = Helper.RenderViewToString(ControllerContext, "_YourCollections", model, true);
            return Json(new { PartialView = partial });
        }

        //
        // GET: /Manage/ManageCollections/_EditCollection

        [Authorize]
        public ActionResult UserCollection(int? id)
        {
            var partial = String.Empty;
            var userId = User.Identity.GetUserId();
            var ycModel = new YourCollectionViewModel
            {
                Collections = db.Collections.ToList(),
                UserCollections = db.UserCollections.Where(uc => uc.UserId == userId).ToList()
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

            //var partial = Helper.RenderViewToString(ControllerContext, "_UserCollection", ucModel, true);
            //return Json(new { PartialView = partial, ScrollTarget = "#user-collection-container" }, JsonRequestBehavior.AllowGet);

            partial = Helper.RenderViewToString(ControllerContext, "_YourCollections", ycModel, true);
            return Json(new { PartialView = partial, RefreshTargets = new { first = "#your-collections-container", second = "#user-collection-container" }, ScrollTarget = "#user-collection-container" }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult ChangeUserCollectionName([Bind(Prefix = "UserCollection")] UserCollection model)
        {
            var userCollection = db.UserCollections.Find(model.Id);
            

            var partial = String.Empty;
            if (!ModelState.IsValid)
            {
                partial = Helper.RenderViewToString(ControllerContext, "_UserCollection", model, true);
                return Json(new { PartialView = partial });
            }

            userCollection.Update("Name", model.Name, db);
            var ucModel = new UserCollectionModel
            {
                Collection = db.Collections.Find(userCollection.CollectionId),
                UserCollection = userCollection
            };
            var ycViewModel = new YourCollectionViewModel
            {
                Collections = db.Collections.ToList(),
                UserCollections = db.UserCollections.Where(uc => uc.UserId == model.UserId).ToList(),
                EditCollection = ucModel
            };

            partial = Helper.RenderViewToString(ControllerContext, "_YourCollections", ycViewModel, true);
            return Json(new { PartialView = partial, RefreshTargets = new { first = "#your-collections-container" } });
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
        public ActionResult DeleteUserCollection(int id)
        {
            var partial = String.Empty;
            UserCollection userCollection = db.UserCollections.Find(id);
            userCollection.Delete(db);

            var userId = User.Identity.GetUserId();
            var ycViewModel = new YourCollectionViewModel
            {
                Collections = db.Collections.ToList(),
                UserCollections = db.UserCollections.Where(uc => uc.UserId == userId).ToList()
            };

            partial = Helper.RenderViewToString(ControllerContext, "_YourCollections", ycViewModel, true);
            return Json(new { PartialView = partial, RefreshTargets = new { first = "#your-collections-container" } });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult UpdateItemCount(int quantity, int index, int ucId)
        {
            var partial = String.Empty;
            UserCollection userCollection = db.UserCollections.Find(ucId);
            userCollection.Update("Quantity", JsonConvert.SerializeObject(new { index, quantity }), db);

            var userId = User.Identity.GetUserId();
            var ycViewModel = new YourCollectionViewModel
            {
                Collections = db.Collections.ToList(),
                UserCollections = db.UserCollections.Where(uc => uc.UserId == userId).ToList()
            };

            partial = Helper.RenderViewToString(ControllerContext, "_YourCollections", ycViewModel, true);
            return Json(new { PartialView = partial, RefreshTargets = new { first = "#your-collections-container" } });
            //return null;
        }

        //
        // GET: /Manage/SwapHistory

        [Authorize]
        public ActionResult SwapHistoryPartial()
        {
            var userId = User.Identity.GetUserId();
            var shModel = new SwapHistoryViewModel
            {
                Swaps = db.Swaps.Where(swap => swap.SenderId == userId || swap.ReceiverId == userId)
                                .Include(swap => swap.Collection)
                                .Include(swap => swap.Sender)
                                .Include(swap => swap.Receiver).ToList()
            };

            var partial = Helper.RenderViewToString(ControllerContext, "_SwapHistory", shModel, true);
            return Json(new { PartialView = partial, RefreshTargets = new { first = "#history-container" } });
        }

        [Authorize]
        public ActionResult ConfirmReceived(int id)
        {
            var userId = User.Identity.GetUserId();
            var swap = db.Swaps.Find(id);
            if (userId == swap.ReceiverId)
            {
                swap.Confirm("receiver", db);
            }
            else
            {
                swap.Confirm("sender", db);
            }

            var shModel = new SwapHistoryViewModel
            {
                Swaps = db.Swaps.Where(s => s.SenderId == userId || s.ReceiverId == userId)
                                .Include(s => s.Collection)
                                .Include(s => s.Sender)
                                .Include(s => s.Receiver).ToList()
            };

            var partial = Helper.RenderViewToString(ControllerContext, "_SwapHistory", shModel, true);
            return Json(new { PartialView = partial, RefreshTargets = new { first = "#history-container" } });
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