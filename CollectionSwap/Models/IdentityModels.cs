using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Newtonsoft.Json;

namespace CollectionSwap.Models
{
    // You can add profile data for the user by adding more properties to your ApplicationUser class, please visit https://go.microsoft.com/fwlink/?LinkID=317594 to learn more.
    public class ApplicationUser : IdentityUser
    {
        public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<ApplicationUser> manager)
        {
            // Note the authenticationType must match the one defined in CookieAuthenticationOptions.AuthenticationType
            var userIdentity = await manager.CreateIdentityAsync(this, DefaultAuthenticationTypes.ApplicationCookie);
            // Add custom user claims here
            return userIdentity;
        }

        public string ChangeEmail(string oldEmail, string newEmail, ApplicationDbContext db)
        {
            var status = string.Empty;
            if (this.Email.ToLower() != oldEmail.ToLower())
            {
                status = "Incorrect email";
            }
            else if (db.Users.Where(u => u.Email.ToLower() == newEmail.ToLower()).Any()) 
            {
                status = "This email already exists";
            }
            else
            {
                this.Email = newEmail.ToLower();
                db.Entry(this).State = EntityState.Modified;
                db.SaveChanges();
            }

            return status;
        }
        public virtual Address Address { get; set; }
        public double Rating
        {
            get
            {
                using (ApplicationDbContext db = new ApplicationDbContext())
                {
                    var rating = db.Feedbacks.Where(f => f.Receiver.Id == this.Id).Select(f => f.Rating).ToList();
                    return rating.Count == 0 ? -1 : rating.Average();
                }
            }
        }
        public string BlockedUsers { get; set; }
        public bool ClosedAccount { get; set; }
        public void HandleBlock(string username, bool isBlocked, ApplicationDbContext db)
        {
            var blockedUser = db.Users.Where(u => u.UserName.ToLower() == username.ToLower()).FirstOrDefault();
            var blockedUsers = this.BlockedUsers == null || this.BlockedUsers == "[]" ? new List<string>() : JsonConvert.DeserializeObject<List<string>>(this.BlockedUsers);
            if (isBlocked)
            {
                if (!blockedUsers.Contains(blockedUser.Id))
                    blockedUsers.Add(blockedUser.Id);
            }
            else
                blockedUsers.Remove(blockedUser.Id);

            this.BlockedUsers = JsonConvert.SerializeObject(blockedUsers);

            db.Entry(this).State = EntityState.Modified;
            db.SaveChanges();
        }
        public bool HasUserBlocked(string username)
        {
            using (ApplicationDbContext db = new ApplicationDbContext())
            {
                string userId = db.Users.Where(u => u.UserName.ToLower().Contains(username.ToLower())).Select(u => u.Id).FirstOrDefault();
                return this.BlockedUsers != null ? this.BlockedUsers.Contains(userId) : false;
            }
        }
        public class CloseAccountResult
        {
            public bool Success { get; set; }
            public string Message { get; set; }
        }
        public CloseAccountResult CloseAccount(ApplicationDbContext db)
        {
            var pendingSwaps = db.Swaps
                .Where(s => (s.Sender.Id == this.Id || s.Receiver.Id == this.Id) &&
                s.Status == "requested" || s.Status == "accepted")
                .ToList();
            // By ensuring the user has not left feedback, we don't unintentionally include'pseudo-completed' swaps
            var confirmedSwaps = db.Swaps
                .Where(s => ((s.Sender.Id == this.Id && s.SenderFeedback == null) || (s.Receiver.Id == this.Id && s.ReceiverFeedback == null)) && s.Status == "confirmed").ToList();


            if (confirmedSwaps.Any())
            {
                return new CloseAccountResult { Success = false, Message = "You cannot close your account while you have confirmed swaps pending." };
            }

            foreach(var swap in pendingSwaps)
            {
                var cancelRequest = new SwapRequestViewModel
                {
                    SwapId = swap.Id,
                    Status = "canceled"
                };
                swap.ProcessSwap(this.Id, cancelRequest, db);
            }

            this.Email = "(closed)" + this.Email;
            this.ClosedAccount = true;
            db.Entry(this).State = EntityState.Modified;
            db.SaveChanges();
            return new CloseAccountResult { Success = true, Message = "Your account has been closed." };
        }
    }

    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext()
            : base("DefaultConnection", throwIfV1Schema: false)
        {
        }

        public static ApplicationDbContext Create()
        {
            return new ApplicationDbContext();
        }

        public DbSet<Collection> Collections { get; set; }
        public DbSet<UserCollection> UserCollections { get; set; }
        public DbSet<Swap> Swaps { get; set; }
        public DbSet<HeldItems> HeldItems { get; set; }
        public DbSet<Address> Addresses { get; set; }
        public DbSet<Feedback> Feedbacks { get; set; }
        public DbSet<Sponsor> Sponsors { get; set; }
    }

    public class ApplicationRole : IdentityRole
    {
        public ApplicationRole() : base() { }
        public ApplicationRole(string name) : base(name) { }
    }
}