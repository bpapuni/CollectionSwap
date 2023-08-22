using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace CollectionSwap.Models
{
    public class FindSwapsViewModel
    {
        public List<ApplicationUser> Users { get; set; }
        public List<Collection> Collections { get; set; }
        public List<UserCollection> UserCollections { get; set; }
        public List<Swap> OfferedSwaps { get; set; }
        public List<Swap> AcceptedSwaps { get; set; }
        public List<Swap> ConfirmedSwaps { get; set; }
        public static FindSwapsViewModel Create(string currentUserId, ApplicationDbContext db)
        {
            FindSwapsViewModel model = new FindSwapsViewModel
            {
                Users = db.Users.ToList(),
                Collections = db.Collections.ToList(),
                UserCollections = db.UserCollections.Where(uc => uc.User.Id == currentUserId).ToList(),
                OfferedSwaps = db.Swaps.Where(swap => swap.Receiver.Id == currentUserId && swap.Status == "offered").ToList(),
                AcceptedSwaps = db.Swaps.Where(swap => swap.Sender.Id == currentUserId && swap.Status == "accepted").ToList(),
                ConfirmedSwaps = db.Swaps.Where(swap => swap.Sender.Id == currentUserId && swap.Status == "confirmed").ToList()
            };

            return model;
        }
    }

    public class SwapViewModel
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public List<string> ItemList { get; set; }
        public string ImagePath { get; set; }
        public List<int> SenderItemIds { get; set; }
        public List<int> ReceiverItemIds { get; set; }
        public int SwapSize { get; set; }
        public string Type { get; set; }
    }

    public class SwapHistoryViewModel
    {
        public List<Swap> Swaps { get; set; }
    }

    public class Swap
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public int CollectionId { get; set; }
        [Required]
        public int SenderUserCollectionId { get; set; }
        [Required]
        public int ReceiverUserCollectionId { get; set; }
        [Required]
        public string SenderId { get; set; }
        [Required]
        public string ReceiverId { get; set; }
        [Required]
        public string SenderItemIdsJSON { get; set; }
        [Required]
        public string ReceiverItemIdsJSON { get; set; }
        [Required]
        public string Status { get; set; }
        [Required]
        public bool SenderConfirmSent { get; set; }
        [Required]
        public bool ReceiverConfirmSent { get; set; }
        [Required]
        public bool SenderConfirmReceieved { get; set; }
        [Required]
        public bool ReceiverConfirmReceieved { get; set; }
        [Required]
        public DateTimeOffset StartDate { get; set; }
        public DateTimeOffset? EndDate { get; set; }
        [ForeignKey("SenderId")]
        public ApplicationUser Sender { get; set; }
        [ForeignKey("ReceiverId")]
        public ApplicationUser Receiver { get; set; }
        [ForeignKey("CollectionId")]
        public Collection Collection { get; set; }
        public string Process(ApplicationDbContext db)
        {
            string response = string.Empty;
            switch (this.Status)
            {
                case "offered":
                    var receiverName = db.Users.Find(this.ReceiverId).UserName;
                    db.Swaps.Add(this);
                    db.SaveChanges();

                    response = $"Offer made, waiting for {receiverName} to accept.";
                    break;

                case "accepted":
                    var senderName = db.Users.Find(this.SenderId).UserName;
                    var receiverItems = db.UserCollections.Find(this.ReceiverUserCollectionId);

                    db.Entry(this).State = EntityState.Modified;
                    HoldItems(this.ReceiverItemIdsJSON, receiverItems, this, db);
                    db.SaveChanges();

                    response = $"Offer accepted, waiting for {senderName} to confirm.";
                    break;

                case "confirmed":
                    var senderItems = db.UserCollections.Find(this.SenderUserCollectionId);

                    db.Entry(this).State = EntityState.Modified;
                    HoldItems(this.SenderItemIdsJSON, senderItems, this, db);
                    db.SaveChanges();

                    response = $"Swap confirmed.";
                    break;

                case "declined":
                    ReleaseItems(this, db);

                    db.Entry(this).State = EntityState.Modified;
                    db.Swaps.Remove(this);
                    db.SaveChanges();

                    response = $"Swap declined.";
                    break;
                default:
                    break;
            }

            return response;
        }
        public void Confirm(string userType, ApplicationDbContext db)
        {
            switch (userType)
            {
                case "sender":
                    this.SenderConfirmReceieved = true;
                    db.Entry(this).State = EntityState.Modified;
                    db.SaveChanges();

                    break;

                case "receiver":
                    this.ReceiverConfirmReceieved = true;
                    db.Entry(this).State = EntityState.Modified;
                    db.SaveChanges();

                    break;
                default:
                    break;
            }
        }

        private void HoldItems(string itemListJSON, UserCollection userCollection, Swap swap, ApplicationDbContext db)
        {
            var deserializedReceiverItems = JsonConvert.DeserializeObject<List<int>>(userCollection.ItemCountJSON);
            var deserializedSwapItems = JsonConvert.DeserializeObject<List<int>>(itemListJSON);

            foreach (var item in deserializedSwapItems)
            {
                deserializedReceiverItems[item] = deserializedReceiverItems[item] - 1;
            }
            userCollection.ItemCountJSON = JsonConvert.SerializeObject(deserializedReceiverItems);

            HeldItems heldItems = new HeldItems
            {
                ItemListJSON = itemListJSON,
                UserCollection = userCollection,
                Swap = swap
            };

            db.Entry(userCollection).State = EntityState.Modified;
            db.HeldItems.Add(heldItems);
            db.SaveChanges();
        }
        private void ReleaseItems(Swap swap, ApplicationDbContext db)
        {
            var heldItems = db.HeldItems.Include("UserCollection").Where(hi => hi.Swap.Id == swap.Id).ToList();

            foreach (var item in heldItems)
            {
                var userCollection = db.UserCollections.Find(item.UserCollection.Id);
                var deserializedReleaseItems = JsonConvert.DeserializeObject<List<int>>(item.ItemListJSON);
                var deserializedItems = JsonConvert.DeserializeObject<List<int>>(userCollection.ItemCountJSON);

                foreach (var deserializedItem in deserializedReleaseItems)
                {
                    deserializedItems[deserializedItem] = deserializedItems[deserializedItem] + 1;
                }

                userCollection.ItemCountJSON = JsonConvert.SerializeObject(deserializedItems);
                db.Entry(userCollection).State = EntityState.Modified;
                db.SaveChanges();
            }

            db.HeldItems.RemoveRange(heldItems);
            db.SaveChanges();
        }
        private void SwapItems(Swap swap, ApplicationDbContext db)
        {
            var heldItems = db.HeldItems.Include("UserCollection").Where(hi => hi.Swap.Id == swap.Id).ToList();

            for (var i = 0; i <= 1; i++)
            {
                var index = i == 0 ? 1 : 0;
                var userCollection = db.UserCollections.Find(heldItems[index].UserCollection.Id);
                var deserializedSwapItems = JsonConvert.DeserializeObject<List<int>>(heldItems[i].ItemListJSON);
                var deserializedItems = JsonConvert.DeserializeObject<List<int>>(userCollection.ItemCountJSON);

                foreach (var deserializedItem in deserializedSwapItems)
                {
                    deserializedItems[deserializedItem] = deserializedItems[deserializedItem] + 1;
                }

                userCollection.ItemCountJSON = JsonConvert.SerializeObject(deserializedItems);
                db.Entry(userCollection).State = EntityState.Modified;
                db.SaveChanges();
            }

            db.HeldItems.RemoveRange(heldItems);
            db.SaveChanges();
        }
    }
}