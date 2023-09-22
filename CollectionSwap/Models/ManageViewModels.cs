using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.Owin.Security;

namespace CollectionSwap.Models
{
    public class IndexViewModel
    {
        public bool HasPassword { get; set; }
        public IList<UserLoginInfo> Logins { get; set; }
        public string PhoneNumber { get; set; }
        public bool TwoFactor { get; set; }
        public bool BrowserRemembered { get; set; }
        public ChangeEmailViewModel ChangeEmail { get; set; }
        public ChangePasswordViewModel ChangePassword { get; set; }
        public Address ChangeAddress { get; set; }
        public List<Collection> Collections { get; set; }
        public List<UserCollection> UserCollections { get; set; }
        public List<Feedback> RecentFeedback { get; set; }
    }

    public class ManageCollectionsViewModel
    {
        public List<Collection> Collections { get; set; }
        public CreateCollectionModel CreateCollection { get; set; }
        public EditCollectionModel EditCollection { get; set; }
    }

    public class YourCollectionViewModel
    {
        public List<Collection> Collections { get; set; }
        public List<UserCollection> UserCollections { get; set; }
        public UserCollection EditCollection { get; set; }
    }

    public class FeedbackViewModel
    {
        public Swap Swap { get; set; }
        public Feedback Feedback { get; set; }
    }

    public class ManageLoginsViewModel
    {
        public IList<UserLoginInfo> CurrentLogins { get; set; }
        public IList<AuthenticationDescription> OtherLogins { get; set; }
    }

    public class FactorViewModel
    {
        public string Purpose { get; set; }
    }

    public class SetPasswordViewModel
    {
        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "New password")]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm new password")]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }
    }

    public class ChangeEmailViewModel
    {
        [Required(ErrorMessage = "This field is required.")]
        [Display(Name = "Current Email")]
        public string OldEmail { get; set; }

        [Required(ErrorMessage = "This field is required.")]
        [EmailWithoutSpecialChars(ErrorMessage = "Email contains an invalid or non-english character.")]
        [Display(Name = "New Email")]
        public string NewEmail { get; set; }

        [Required(ErrorMessage = "This field is required.")]
        [EmailWithoutSpecialChars(ErrorMessage = "Email contains an invalid or non-english character.")]
        [Display(Name = "Confirm new Email")]
        [CaseInsensitiveCompare("NewEmail", ErrorMessage = "The new email and confirmation email do not match.")]
        public string ConfirmEmail { get; set; }
    }

    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "This field is required.")]
        [DataType(DataType.Password)]
        [Display(Name = "Current password")]
        public string OldPassword { get; set; }

        [Required(ErrorMessage = "This field is required.")]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [RegularExpression(@"^(?=.*[A-Z])(?=.*\d)(?=.*[`@$!%*?&])[\x20-\x7E]{6,}$",
    ErrorMessage = "Password must be 6+ characters, include an uppercase letter, a number, a symbol, and no spaces.")]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "This field is required.")]
        [DataType(DataType.Password)]
        [RegularExpression(@"^(?=.*[A-Z])(?=.*\d)(?=.*[`@$!%*?&])[\x20-\x7E]{6,}$",
    ErrorMessage = "Password must be 6+ characters, include an uppercase letter, a number, a symbol, and no spaces.")]
        [Display(Name = "Confirm New Password")]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }
    }

    public class Address
    {
        [Key]
        public int Id { get; set; }
        public string UserId { get; set; }
        [Required(ErrorMessage = "This field is required.")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; }
        [Display(Name = "Company Name (optional)")]
        public string CompanyName { get; set; }
        [Required(ErrorMessage = "This field is required.")]
        [Display(Name = "Address Line 1")]
        public string LineOne { get; set; }
        [Display(Name = "Address Line 2 (optional)")]
        public string LineTwo { get; set; }
        [Required(ErrorMessage = "This field is required.")]
        [Display(Name = "Post Code")]
        public string PostCode { get; set; }
        [Required(ErrorMessage = "This field is required.")]
        public string City { get; set; }
        [Required]
        public DateTimeOffset Created { get; set; }

        public class CreateAddressResult
        {
            public bool Succeeded { get; set; }
            public string Error { get; set; }
        }

        public CreateAddressResult CreateAddress(string userId, ApplicationDbContext db)
        {
            try
            {
                var lastAddress = db.Addresses.OrderByDescending(a => a.Created)
                                              .FirstOrDefault(a => a.UserId == userId);

                if (lastAddress != null && 
                    lastAddress.FullName == this.FullName &&
                    lastAddress.CompanyName == this.CompanyName &&
                    lastAddress.LineOne == this.LineOne &&
                    lastAddress.LineTwo == this.LineTwo &&
                    lastAddress.PostCode == this.PostCode &&
                    lastAddress.City == this.City) 
                {
                    return new CreateAddressResult { Succeeded = false, Error = "This is already your current address." };
                }
                this.UserId = userId;
                this.Created = DateTimeOffset.UtcNow;
                db.Addresses.Add(this);

                var user = db.Users.Find(userId);

                user.Address = this;
                db.Entry(user).State = EntityState.Modified;

                db.SaveChanges();
                return new CreateAddressResult { Succeeded = true };
            }
            catch (Exception ex)
            {
                return new CreateAddressResult { Succeeded = false, Error = ex.Message };
            }
        }
    }

    public class AddPhoneNumberViewModel
    {
        [Required]
        [Phone]
        [Display(Name = "Phone Number")]
        public string Number { get; set; }
    }

    public class VerifyPhoneNumberViewModel
    {
        [Required]
        [Display(Name = "Code")]
        public string Code { get; set; }

        [Required]
        [Phone]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }
    }

    public class ConfigureTwoFactorViewModel
    {
        public string SelectedProvider { get; set; }
        public ICollection<System.Web.Mvc.SelectListItem> Providers { get; set; }
    }
}