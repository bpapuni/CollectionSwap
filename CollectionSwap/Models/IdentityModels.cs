﻿using System;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;

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