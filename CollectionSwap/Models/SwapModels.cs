using CollectionSwap.Models;
using Microsoft.AspNet.Identity;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace CollectionSwap.Models
{
    public class FindSwapsViewModel
    {
        public List<ApplicationUser> Users { get; set; }
        public List<Collection> Collections { get; set; }
        public List<UserCollection> UserCollections { get; set; }
        public List<Swap> MatchingSwaps { get; set; }
        public List<Swap> OfferedSwaps { get; set; }
        public List<Swap> AcceptedSwaps { get; set; }
        public List<Swap> ConfirmedSwaps { get; set; }
        public List<Feedback> Feedbacks { get; set; }
    }

    public class SwapViewModel
    {
        public Swap Swap { get; set; }
        public Address Address { get; set; }
        public double Rating { get; set; }
        public string DuplicateSwapItems { get; set; }
    }

    public class SwapHistoryViewModel
    {
        public List<Swap> Swaps { get; set; }
        public FeedbackViewModel Feedback { get; set; }
        public SwapViewModel Offer { get; set; }
    }

    public class Swap
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public int CollectionId { get; set; }
        [Required]
        public string SenderId { get; set; }
        [Required]
        public int SenderCollectionId { get; set; }
        [Required]
        public string SenderRequestedItems { get; set; }
        [Required]
        public bool SenderConfirmSent { get; set; }
        [Required]
        public bool SenderConfirmReceived { get; set; }
        [Required]
        public bool SenderFeedbackSent { get; set; }
        [Required]
        public string ReceiverId { get; set; }
        [Required]
        public int ReceiverCollectionId { get; set; }
        [Required]
        public string ReceiverRequestedItems { get; set; }
        [Required]
        public bool ReceiverConfirmSent { get; set; }
        [Required]
        public bool ReceiverConfirmReceived { get; set; }
        [Required]
        public bool ReceiverFeedbackSent { get; set; }
        public int SwapSize { get; set; }
        [Required]
        public string Status { get; set; }
        [Required]
        public DateTimeOffset StartDate { get; set; }
        public DateTimeOffset? EndDate { get; set; }
        [ForeignKey("CollectionId")]
        public Collection Collection { get; set; }
        [ForeignKey("SenderId")]
        public ApplicationUser Sender { get; set; }
        [ForeignKey("SenderCollectionId")]
        public UserCollection SenderCollection { get; set; }
        [ForeignKey("ReceiverId")]
        public ApplicationUser Receiver { get; set; }
        [ForeignKey("ReceiverCollectionId")]
        public UserCollection ReceiverCollection { get; set; }
        public class ProcessSwapResult
        {
            public bool Succeeded { get; set; }
            public string SuccessType { get; set; }
            public string Error { get; set; }
        }
        public async Task<ProcessSwapResult> ProcessAsync(string userId, SwapRequestViewModel request, ApplicationDbContext db)
        {
            try
            {
                string response = string.Empty;
                switch (request.Status)
                {
                    case "offered":
                        this.CollectionId = request.CollectionId;
                        this.SenderCollectionId = request.SenderUserCollectionId;
                        this.ReceiverCollectionId = request.ReceiverUserCollectionId;
                        this.SenderId = userId;
                        this.ReceiverId = request.ReceiverId;
                        this.SenderRequestedItems = request.SenderItems;
                        this.ReceiverRequestedItems = request.RequestedItems;
                        this.SwapSize = request.SwapSize;
                        this.Status = request.Status;
                        this.StartDate = request.StartDate;

                        db.Swaps.Add(this);
                        await db.SaveChangesAsync();
                        return new ProcessSwapResult { Succeeded = true, SuccessType = "offered" };

                    case "accepted":
                        this.SenderRequestedItems = request.SenderItems;
                        this.Status = request.Status;
                        db.Entry(this).State = EntityState.Modified;

                        HoldItems(this.ReceiverRequestedItems, db.UserCollections.Find(this.ReceiverCollectionId), this, db);
                        await db.SaveChangesAsync();
                        return new ProcessSwapResult { Succeeded = true, SuccessType = "accepted" };

                    case "confirmed":
                        this.ReceiverRequestedItems = request.RequestedItems;
                        this.Status = request.Status;
                        db.Entry(this).State = EntityState.Modified;

                        HoldItems(this.SenderRequestedItems, db.UserCollections.Find(this.SenderCollectionId), this, db);
                        await db.SaveChangesAsync();
                        return new ProcessSwapResult { Succeeded = true, SuccessType = "confirmed" };

                    case "cancel":
                    case "decline":

                        ReleaseItems(this, db);
                        db.Swaps.Remove(this);
                        await db.SaveChangesAsync();
                        return new ProcessSwapResult { Succeeded = true, SuccessType = request.Status };

                    default:
                        break;
                }

                return new ProcessSwapResult { Succeeded = false };
            }
            catch (Exception ex)
            {
                return new ProcessSwapResult { Succeeded = false, Error = ex.Message };
            }
        }
        public void Confirm(string type, string userId, ApplicationDbContext db)
        {
            var userType = String.Empty;
            if (this.SenderId == userId)
            {
                userType = "sender";

                if (type == "sent")
                {
                    this.SenderConfirmSent = true;
                }
                else if (type == "received")
                {
                    this.SenderConfirmReceived = true;
                }
            }
            else if (this.ReceiverId == userId)
            {
                userType = "receiver";

                if (type == "sent")
                {
                    this.ReceiverConfirmSent = true;
                }
                else if (type == "received")
                {
                    this.ReceiverConfirmReceived = true;
                }
            }

            if (this.SenderConfirmSent &&
                this.SenderConfirmReceived &&
                this.ReceiverConfirmSent &&
                this.ReceiverConfirmReceived)
            {
                this.Status = "completed";
            }

            db.Entry(this).State = EntityState.Modified;
            db.SaveChanges();
            if (type == "received")
            {
                SwapItems(this, userType, db);
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
        private void SwapItems(Swap swap, string userType, ApplicationDbContext db)
        {
            var heldItems = new HeldItems();
            var userCollection = new UserCollection();
            switch (userType)
            {
                case "sender":
                    // Find the items that DO NOT belong to to sender
                    heldItems = db.HeldItems.Include("UserCollection").Where(
                                                                            hi => hi.Swap.Id == swap.Id &&
                                                                            hi.UserCollection.UserId != swap.SenderId)
                                                                            .FirstOrDefault();

                    // Define the User Collection that will be getting updated as the senders
                    userCollection = db.UserCollections.Find(swap.SenderCollectionId);
                    break;

                case "receiver":
                    // Find the items that DO NOT belong to to receiver
                    heldItems = db.HeldItems.Include("UserCollection").Where(
                                                                        hi => hi.Swap.Id == swap.Id &&
                                                                        hi.UserCollection.UserId != swap.ReceiverId)
                                                                        .FirstOrDefault();

                    // Define the User Collection that will be getting updated as the receivers
                    userCollection = db.UserCollections.Find(swap.ReceiverCollectionId);
                    break;
                default:
                    break;
            }

            var deserializedSwapItems = JsonConvert.DeserializeObject<List<int>>(heldItems.ItemListJSON);
            var deserializedItems = JsonConvert.DeserializeObject<List<int>>(userCollection.ItemCountJSON);

            foreach (var deserializedItem in deserializedSwapItems)
            {
                deserializedItems[deserializedItem] += 1;
            }

            userCollection.ItemCountJSON = JsonConvert.SerializeObject(deserializedItems);
            db.Entry(userCollection).State = EntityState.Modified;

            db.HeldItems.Remove(heldItems);
            db.SaveChanges();
        }
        public string Validate(string userId, List<Swap> usersSwaps, ApplicationDbContext db)
        {
            var offeredSwaps = usersSwaps.Where(swap => swap.Status == "offered").ToList();
            var acceptedSwaps = usersSwaps.Where(swap => swap.Status == "accepted").ToList();
            var yourItemList = SenderId == userId ? JsonConvert.DeserializeObject<List<int>>(SenderCollection.ItemCountJSON) : JsonConvert.DeserializeObject<List<int>>(ReceiverCollection.ItemCountJSON);
            var DuplicateItems = new List<int>();

            // pseudocode
            // if offered and youre receiver check for duplicates
            // if accepted and youre sender check for duplicates

            if (Status == "offered" && Receiver.Id == userId)
            {
                var offeredItems = new List<int>();
                foreach(var swap in offeredSwaps)
                {
                    var senderRequestedItems = JsonConvert.DeserializeObject<List<int>>(swap.ReceiverRequestedItems);
                    offeredItems.AddRange(senderRequestedItems);
                }

                var offeredItemsCount = new int[yourItemList.Count];
                foreach (var item in offeredItems)
                {
                    offeredItemsCount[item]++;
                }

                for (int i = 0; i < yourItemList.Count; i++)
                {
                    // check if you have enough of each item to swap
                    if (yourItemList[i] > 0 && offeredItemsCount[i] > 0 && yourItemList[i] - offeredItemsCount[i] < 1)
                    {
                        DuplicateItems.Add(i);
                    }
                }
            }

            if (Status == "accepted" && Sender.Id == userId)
            {
                var acceptedItems = new List<int>();
                foreach(var swap in acceptedSwaps)
                {
                    var receiverRequestedItems = JsonConvert.DeserializeObject<List<int>>(swap.SenderRequestedItems);
                    acceptedItems.AddRange(receiverRequestedItems);
                }

                var acceptedItemsCount = new int[yourItemList.Count];
                foreach(var item in acceptedItems)
                {
                    acceptedItemsCount[item]++;
                }

                for(int i = 0; i < yourItemList.Count; i++)
                {
                    // check if you have enough of each item to swap
                    if (yourItemList[i] > 0 && acceptedItemsCount[i] > 0 && yourItemList[i] - acceptedItemsCount[i] < 1)
                    {
                        DuplicateItems.Add(i);
                    }
                }
            }

            return JsonConvert.SerializeObject(DuplicateItems);
        }
    }

    public class SwapRequestViewModel
    {
        public int SwapId { get; set; }
        public string ReceiverId { get; set; }
        public int CollectionId { get; set; }
        public int SenderUserCollectionId { get; set; }
        public int ReceiverUserCollectionId { get; set; }
        public string SenderItems { get; set; }
        public string RequestedItems { get; set; }
        public DateTimeOffset StartDate { get; set; }
        public int SwapSize { get; set; }
        public string Status { get; set; }
    }

    public class Feedback
    {
        public int Id { get; set; }
        public int SwapId { get; set; }
        public string SenderId { get; set; }
        public string ReceiverId { get; set; }
        [Required(ErrorMessage = "You must select a rating")]
        public int Rating { get; set; }
        public string PositiveFeedback { get; set; }
        public string NeutralFeedback { get; set; }
        public string NegativeFeedback { get; set; }
        public Feedback Create(string userId, ApplicationDbContext db)
        {
            if (db.Feedbacks.Find(this.Id) != null)
            {
                return this;
            }
            this.SenderId = userId;
            this.ReceiverId = db.Swaps.Find(this.SwapId).SenderId == userId ? db.Swaps.Find(this.SwapId).ReceiverId : db.Swaps.Find(this.SwapId).SenderId;

            db.Feedbacks.Add(this);
            db.SaveChanges();

            return this;
        }
    }
}