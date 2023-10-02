using CollectionSwap.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace CollectionSwap.Controllers
{
    public class RemoteValidationController : Controller
    {
        public JsonResult IsUsernameAvailable()
        {
            string username = Request.QueryString["RegisterViewModel.Username"];

            using (ApplicationDbContext db = new ApplicationDbContext())
            {
                bool result = !db.Users.Any(u => u.UserName.ToLower() == username.ToLower());
                return Json(result, JsonRequestBehavior.AllowGet);
            }
        }
        public JsonResult IsEmailAvailable()
        {
            string email = Request.QueryString["RegisterViewModel.Email"];

            using (ApplicationDbContext db = new ApplicationDbContext())
            {
                bool result = !db.Users.Any(u => u.Email.ToLower() == email.ToLower());
                return Json(result, JsonRequestBehavior.AllowGet);
            }
        }
    }
}