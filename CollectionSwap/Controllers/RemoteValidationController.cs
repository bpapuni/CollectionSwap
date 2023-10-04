using CollectionSwap.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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

        public JsonResult IsPasswordValid()
        {
            string password = Request.QueryString["RegisterViewModel_Password"];

            bool hasUpper = false;
            bool hasNumber = false;
            bool hasSpecial = false;
            bool hasLength = false;

            if (password.Length >= 6)
            {
                hasLength = true;
            }
            foreach (char c in password)
            {
                if (char.IsUpper(c))
                {
                    hasUpper = true;
                }
                if (char.IsNumber(c))
                {
                    hasNumber = true;
                }
                Regex symbol = new Regex(@"^[.#@$!]*$");
                if (symbol.IsMatch(c.ToString()))
                {
                    hasSpecial = true;
                }
            }

            var validationResults = new[]
            {
                new { Rule = "upper", IsValid = hasUpper },
                new { Rule = "number", IsValid = hasNumber },
                new { Rule = "special", IsValid = hasSpecial },
                new { Rule = "length", IsValid = hasLength }
            };

            return Json(validationResults, JsonRequestBehavior.AllowGet);
        }
    }
}