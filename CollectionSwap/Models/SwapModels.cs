using CollectionSwap.Models;
using Microsoft.AspNet.Identity;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace CollectionSwap.Models
{
    public class ValidateResult
    {
        public List<int> LowInventoryItems = new List<int>();
        public List<int> DuplicateRequestItems = new List<int>();
        public List<int> DuplicateOfferedItems = new List<int>();
        public List<int> DuplicateAcceptedItems = new List<int>();
        public bool IsValid = true;
    }

    public class FindSwapsViewModel
    {
        //public List<ApplicationUser> Users { get; set; }
        public List<Collection> Collections { get; set; }
        public List<UserCollection> UserCollections { get; set; }
        public List<Swap> UserSwaps { get; set; }
        public List<SwapViewModel> MatchingSwapViews { get; set; }
        public List<Feedback> Feedbacks { get; set; }
    }

    public class SwapViewModel
    {
        public Swap Swap { get; set; }
        public Feedback Feedback { get; set; }
        public Address Address { get; set; }
        public double Rating { get; set; }
        public ValidateResult Validation { get; set; }
    }

    public class SwapHistoryViewModel
    {
        public List<Swap> Swaps { get; set; }
        public SwapViewModel Offer { get; set; }
        public List<Feedback> UserFeedbacks { get; set; }
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
        public virtual Feedback SenderFeedback { get; set; }
        public bool SenderDisplaySwap { get; set; } = true;
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
        public virtual Feedback ReceiverFeedback { get; set; }
        public bool ReceiverDisplaySwap { get; set; } = true;
        public int SwapSize { get; set; }
        [Required]
        public string Status { get; set; }
        [Required]
        public DateTimeOffset StartDate { get; set; }
        public DateTimeOffset? EndDate { get; set; }
        [ForeignKey("CollectionId")]
        public virtual Collection Collection { get; set; }
        [ForeignKey("SenderId")]
        public virtual ApplicationUser Sender { get; set; }
        [ForeignKey("SenderCollectionId")]
        public virtual UserCollection SenderCollection { get; set; }
        [ForeignKey("ReceiverId")]
        public virtual ApplicationUser Receiver { get; set; }
        [ForeignKey("ReceiverCollectionId")]
        public virtual UserCollection ReceiverCollection { get; set; }
        public class ProcessSwapResult
        {
            public bool Succeeded { get; set; }
            public string SuccessType { get; set; }
            public string Error { get; set; }
        }
        public ProcessSwapResult ProcessSwap(string userId, SwapRequestViewModel request, ApplicationDbContext db)
        {
            try
            {
                string response = string.Empty;
                var senderItemCount = new List<int>();
                var isCharity = request.Status != "offered" && (this.SenderRequestedItems == null || this.SenderRequestedItems == "[]" || this.ReceiverRequestedItems == null || this.ReceiverRequestedItems == "[]");
                var swapStatus = isCharity ? $"charity-{request.Status}" : request.Status;
                switch (swapStatus)
                {
                    case "charity-requested":
                        senderItemCount = JsonConvert.DeserializeObject<List<int>>(db.UserCollections.Where(uc => uc.Id == request.SenderUserCollectionId).FirstOrDefault().ItemCountJSON);
                        senderItemCount = senderItemCount
                                            .SelectMany((value, index) => Enumerable.Repeat(index, value))
                                            .ToList();

                        this.CollectionId = request.CollectionId;
                        this.SenderCollectionId = request.SenderUserCollectionId;
                        this.ReceiverCollectionId = request.ReceiverUserCollectionId;
                        this.SenderId = request.ReceiverId;                                         // Sender and receiver are switched for donated items
                        this.ReceiverId = userId;
                        this.SenderRequestedItems = JsonConvert.SerializeObject(senderItemCount);   // Snapshot of the items the sender has on offer 
                        this.ReceiverRequestedItems = JsonConvert.SerializeObject(new List<int>());
                        this.SwapSize = 0;
                        this.Status = request.Status;
                        this.StartDate = request.StartDate;

                        db.Swaps.Add(this);
                        break;

                    case "charity-confirmed":
                        var declinedSwaps = db.Swaps.Where(s => s.Id != this.Id && s.SenderCollectionId == this.SenderCollectionId && s.Status == "charity").ToList();

                        // Decline all requests that weren't accepted
                        foreach (var swap in declinedSwaps)
                        {
                            swap.SenderDisplaySwap = false;
                            db.Entry(this).State = EntityState.Modified;
                            db.SaveChanges();

                            var declineRequest = new SwapRequestViewModel
                            {
                                SwapId = swap.Id,
                                Status = "declined"
                            };
                            swap.ProcessSwap(userId, declineRequest, db);
                        }

                        senderItemCount = JsonConvert.DeserializeObject<List<int>>(db.UserCollections.Where(uc => uc.Id == request.SenderUserCollectionId).FirstOrDefault().ItemCountJSON);
                        senderItemCount = senderItemCount
                                            .SelectMany((value, index) => Enumerable.Repeat(index, value))
                                            .ToList();

                        this.SenderRequestedItems = JsonConvert.SerializeObject(senderItemCount);
                        this.Status = request.Status;
                        db.Entry(this).State = EntityState.Modified;

                        HoldItems(this.SenderRequestedItems, db.UserCollections.Find(this.SenderCollectionId), this, db);
                        break;

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
                        break;

                    case "accepted":
                        this.SenderRequestedItems = request.SenderItems;
                        this.Status = request.Status;
                        db.Entry(this).State = EntityState.Modified;

                        HoldItems(this.ReceiverRequestedItems, db.UserCollections.Find(this.ReceiverCollectionId), this, db);
                        break;

                    case "confirmed":
                        this.ReceiverRequestedItems = request.RequestedItems;
                        this.Status = request.Status;
                        db.Entry(this).State = EntityState.Modified;

                        HoldItems(this.SenderRequestedItems, db.UserCollections.Find(this.SenderCollectionId), this, db);
                        break;
                    case "charity-canceled":
                    case "canceled":
                        if (this.Status == "offered" || this.Status == "requested")
                        {
                            this.SenderDisplaySwap = false;
                            this.ReceiverDisplaySwap = false;
                        }
                        else if (userId == this.SenderId)
                        {
                            this.SenderDisplaySwap = false;
                        }
                        else if (userId == this.ReceiverId)
                        {
                            this.ReceiverDisplaySwap = false;
                        }
                        ReleaseItems(this, db);
                        this.Status = request.Status;
                        db.Entry(this).State = EntityState.Modified;
                        break;

                    case "declined":
                        ReleaseItems(this, db);
                        this.Status = request.Status;
                        db.Entry(this).State = EntityState.Modified;
                        break;

                    default:
                        break;
                }

                db.SaveChanges();
                return new ProcessSwapResult { Succeeded = true, SuccessType = swapStatus };
            }
            catch (Exception ex)
            {
                return new ProcessSwapResult { Succeeded = false, Error = ex.Message };
            }
        }
        public void Confirm(string type, string userId, ApplicationDbContext db)
        {
            var isCharity = this.SenderRequestedItems == "[]" || this.ReceiverRequestedItems == "[]";
            var userType = String.Empty;
            if (userId == this.SenderId)
            {
                userType = "sender";

                // If user has clicked the sent items checkbox
                if (type == "sent")
                {
                    this.SenderConfirmSent = true;
                    // If this was a charitable swap, flag sender sender confirm received and receiver confirm sent as true
                    // as these actions do not need to be manually completed
                    this.SenderConfirmReceived = isCharity ? true : this.SenderConfirmReceived;
                    this.ReceiverConfirmSent = isCharity ? true : this.ReceiverConfirmSent;

                    if (isCharity)
                    {
                        db.UserCollections.Find(this.SenderCollectionId).Delete(db);
                    }
                }
                // If user has clicked the received items checkbox
                else if (type == "received")
                {
                    this.SenderConfirmReceived = true;
                }
            }
            else if (userId == this.ReceiverId)
            {
                userType = "receiver";

                // If user has clicked the sent items checkbox
                if (type == "sent")
                {
                    this.ReceiverConfirmSent = true;
                }
                // If user has clicked the received items checkbox
                else if (type == "received")
                {
                    this.ReceiverConfirmReceived = true;
                }
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
        public ValidateResult Validate(string userId, ApplicationDbContext db)
        {
            var result = new ValidateResult();
            var senderRequestedItems = JsonConvert.DeserializeObject<List<int>>(this.SenderRequestedItems);     // Items requested from (not by) the sender
            var receiverRequestedItems = JsonConvert.DeserializeObject<List<int>>(this.ReceiverRequestedItems); // Items requested from (not by) the receiver
            // Find all swaps (not including this swap) where this swaps user collection id is involved            
            var pendingSwaps = db.Swaps
                .Include("Collection")
                .Include("Sender")
                .Include("Receiver")
                .Where(swap => this.Id != swap.Id && (this.SenderCollectionId == swap.SenderCollectionId || this.SenderCollectionId == swap.ReceiverCollectionId || this.ReceiverCollectionId == swap.SenderCollectionId || this.ReceiverCollectionId == swap.ReceiverCollectionId) && (swap.Status == "offered" || swap.Status == "accepted"))
                .ToList();

            if (this.Status == "swap") // Check if matching swap contains an item already requested in another swap by the sender
            {
                // Checks if sender has already requested an item
                foreach (var item in receiverRequestedItems)
                {
                    // Creates a list of all the items requested by the sender in their other swaps
                    var pendingSwapItems = pendingSwaps.SelectMany(swap => JsonConvert.DeserializeObject<List<int>>(swap.ReceiverRequestedItems)).ToList();

                    // If any items from 'this' swap are within the created list they are duplicate request items
                    if (pendingSwapItems.Contains(item))
                    {
                        result.DuplicateRequestItems.Add(item);
                    }
                }

                // Checks if sender has enough copies of an item
                foreach (var item in senderRequestedItems) // For each item in items offered to receiver
                {
                    // Creates a list of all the items offered by the sender in their other swaps
                    var pendingSwapItems = pendingSwaps.SelectMany(swap => swap.Sender.Id == userId ? JsonConvert.DeserializeObject<List<int>>(swap.SenderRequestedItems) : JsonConvert.DeserializeObject<List<int>>(swap.ReceiverRequestedItems)).ToList();
                    var sendersItems = JsonConvert.DeserializeObject<List<int>>(db.UserCollections.Find(this.SenderCollectionId).ItemCountJSON);
                    // Calculates the number of times an item has been offered
                    var offeredItemCount = pendingSwapItems.Count(i => i == item);

                    // Checks if the sender has less inventory of an item than has been offered
                    if (sendersItems[item] <= offeredItemCount + 1)
                    {
                        result.LowInventoryItems.Add(item);
                    }
                }
            }
            else if (this.Status == "offered") // Check if receiver has items available to accept swap
            {
                if (userId == this.SenderId)
                {
                    // Checks if sender has already requested an item
                    foreach (var item in receiverRequestedItems)
                    {
                        // Creates a list of all the items requested by the sender in their other swaps
                        var pendingSwapItems = pendingSwaps.SelectMany(swap => JsonConvert.DeserializeObject<List<int>>(swap.ReceiverRequestedItems)).ToList();

                        // If any items from 'this' swap are within the created list they are duplicate request items
                        if (pendingSwapItems.Contains(item))
                        {
                            result.DuplicateRequestItems.Add(item);
                        }
                    }
                    
                    // Checks if sender has enough copies of an item
                    foreach (var item in senderRequestedItems) // For each item in items offered to receiver
                    {
                        // Creates a list of all the items offered by the sender in their other swaps
                        var pendingSwapItems = pendingSwaps.SelectMany(swap => JsonConvert.DeserializeObject<List<int>>(swap.SenderRequestedItems)).ToList();
                        var sendersItems = JsonConvert.DeserializeObject<List<int>>(db.UserCollections.Find(this.SenderCollectionId).ItemCountJSON);
                        // Calculates the number of times an item has been offered
                        var offeredItemCount = pendingSwapItems.Count(i => i == item);

                        // Checks if the sender has less inventory of an item than has been offered
                        if (sendersItems[item] <= offeredItemCount + 1)
                        {
                            result.LowInventoryItems.Add(item);
                        }
                    }
                }
                else if (userId == this.ReceiverId)
                {
                    // Checks if receiver has already requested an item
                    foreach (var item in senderRequestedItems)
                    {
                        // Creates a list of all the items requested by the sender in their other swaps
                        var receiversPendingSwapItems = pendingSwaps.SelectMany(swap => JsonConvert.DeserializeObject<List<int>>(swap.SenderRequestedItems)).ToList();

                        // If any items from 'this' swap are within the created list they are duplicate request items
                        if (receiversPendingSwapItems.Contains(item))
                        {
                            result.DuplicateRequestItems.Add(item);
                        }
                    }

                    // Checks if receiver has enough copies of an item
                    foreach (var item in receiverRequestedItems) // For each item in items offered to sender
                    {
                        // Creates a list of all the items offered by the receiver in their other swaps
                        var receiversPendingSwapItems = pendingSwaps.SelectMany(swap => JsonConvert.DeserializeObject<List<int>>(swap.ReceiverRequestedItems)).ToList();
                        var receiversItems = JsonConvert.DeserializeObject<List<int>>(db.UserCollections.Find(this.ReceiverCollectionId).ItemCountJSON);
                        // Calculates the number of times an item has been offered
                        var offeredItemCount = receiversPendingSwapItems.Count(i => i == item);

                        // Checks if the receiver has less inventory of an item than has been offered
                        if (receiversItems[item] <= offeredItemCount + 1)
                        {
                            result.LowInventoryItems.Add(item);
                            result.IsValid = false;
                        }
                    }
                }
            }
            else if (this.Status == "accepted") // Check if sender has items available to confirm swap
            {
                if (userId == this.SenderId)
                {
                    // Checks if sender has already requested an item
                    foreach (var item in receiverRequestedItems)
                    {
                        // Creates a list of all the items requested by the sender in their other swaps
                        var pendingSwapItems = pendingSwaps.SelectMany(swap => JsonConvert.DeserializeObject<List<int>>(swap.ReceiverRequestedItems)).ToList();

                        // If any items from 'this' swap are within the created list they are duplicate request items
                        if (pendingSwapItems.Contains(item))
                        {
                            result.DuplicateRequestItems.Add(item);
                        }
                    }

                    // Checks if sender has enough copies of an item
                    foreach (var item in senderRequestedItems) // For each item in items offered to receiver
                    {
                        // Creates a list of all the items offered by the sender in their other swaps
                        var pendingSwapItems = pendingSwaps.SelectMany(swap => JsonConvert.DeserializeObject<List<int>>(swap.SenderRequestedItems)).ToList();
                        var sendersItems = JsonConvert.DeserializeObject<List<int>>(db.UserCollections.Find(this.SenderCollectionId).ItemCountJSON);
                        // Calculates the number of times an item has been offered
                        var offeredItemCount = pendingSwapItems.Count(i => i == item);

                        // Checks if the sender has less inventory of an item than has been offered
                        if (sendersItems[item] <= offeredItemCount + 1)
                        {
                            result.LowInventoryItems.Add(item);
                        }
                    }
                }
                else if (userId == this.ReceiverId)
                {
                    // Checks if receiver has already requested an item
                    foreach (var item in senderRequestedItems)
                    {
                        // Creates a list of all the items requested by the sender in their other swaps
                        var receiversPendingSwapItems = pendingSwaps.SelectMany(swap => JsonConvert.DeserializeObject<List<int>>(swap.SenderRequestedItems)).ToList();

                        // If any items from 'this' swap are within the created list they are duplicate request items
                        if (receiversPendingSwapItems.Contains(item))
                        {
                            result.DuplicateRequestItems.Add(item);
                        }
                    }

                    // Checks if receiver has enough copies of an item
                    foreach (var item in receiverRequestedItems) // For each item in items offered to sender
                    {
                        // Creates a list of all the items offered by the receiver in their other swaps
                        var receiversPendingSwapItems = pendingSwaps.SelectMany(swap => JsonConvert.DeserializeObject<List<int>>(swap.ReceiverRequestedItems)).ToList();
                        var receiversItems = JsonConvert.DeserializeObject<List<int>>(db.UserCollections.Find(this.ReceiverCollectionId).ItemCountJSON);
                        // Calculates the number of times an item has been offered
                        var offeredItemCount = receiversPendingSwapItems.Count(i => i == item);

                        // Checks if the receiver has less inventory of an item than has been offered
                        if (receiversItems[item] <= offeredItemCount + 1)
                        {
                            result.LowInventoryItems.Add(item);
                            result.IsValid = false;
                        }
                    }
                }
            }

            return result;
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
        [Required(ErrorMessage = "You must select a rating")]
        public int Rating { get; set; }
        public string Comments { get; set; }
        public DateTimeOffset DatePlaced { get; set; }
        public virtual ApplicationUser Sender { get; set; }
        public virtual ApplicationUser Receiver { get; set; }
        public static List<string> PositiveComments
        {
            get
            {
                return new List<string>
                {
                    "Swap accepted quickly",
                    "Items arrived quickly",
                    "Items packaged well",
                    "Items in good condition",
                    "Swapper donated cards",
                    "Would gladly swap with again",
                };
            }
        }
        public static List<string> NegativeComments
        {
            get
            {
                return new List<string>
                {
                    "Swap took a long time to be accepted",
                    "Items took too long to arrive",
                    "Items packaged poorly",
                    "Items in poor condition",
                    "Expected items were missing",
                    "Would not swap with again",
                    "User came to my address",
                };
            }
        }
        public Feedback Create(string userId, ApplicationDbContext db)
        {
            var comments = JsonConvert.DeserializeObject<List<string>>(this.Comments);

            // Guard clause that prevents the user from submitting a comment not contained within our feedback options
            foreach (var comment in comments) 
            { 
                if (!PositiveComments.Contains(comment) && !NegativeComments.Contains(comment))
                {
                    return null;
                }
            }

            if (db.Feedbacks.Find(this.Id) != null)
            {
                return this;
            }

            var swap = db.Swaps.Find(this.SwapId);
            var isCharity = swap.ReceiverRequestedItems == "[]";

            this.Sender = db.Users.Find(userId);
            this.Receiver = db.Users.Find(swap.SenderId == userId ? swap.ReceiverId : swap.SenderId);

            if (swap.Sender.Id == userId)
            {
                swap.SenderFeedback = this;
            } 
            else if (swap.ReceiverId == userId)
            {
                swap.ReceiverFeedback = this;
            }

            swap.Status = (swap.SenderFeedback != null && swap.ReceiverFeedback != null) || (isCharity && swap.ReceiverFeedback != null) ? "completed" : swap.Status;
            db.Entry(swap).State = EntityState.Modified;

            this.DatePlaced = DateTime.UtcNow;
            db.Feedbacks.Add(this);
            db.SaveChanges();

            return this;
        }
    }
}