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
        public List<ApplicationUser> Users { get; set; }
        public List<Collection> Collections { get; set; }
        public List<UserCollection> UserCollections { get; set; }
        public List<Swap> UserSwaps { get; set; }
        public List<Swap> MatchingSwaps { get; set; }
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
        [Required]
        public bool ReceiverFeedbackSent { get; set; }
        public bool ReceiverDisplaySwap { get; set; } = true;
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
                var senderItemCount = new List<int>();
                var isCharity = this.SenderRequestedItems == null || this.SenderRequestedItems == "[]" || this.ReceiverRequestedItems == null || this.ReceiverRequestedItems == "[]";
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
                        this.SenderId = request.ReceiverId;                                                 // Sender and receiver are switched for donated items
                        this.ReceiverId = userId;
                        this.SenderRequestedItems = JsonConvert.SerializeObject(senderItemCount);           // Snapshot of the items the sender has on offer 
                        this.ReceiverRequestedItems = JsonConvert.SerializeObject(new List<int>());
                        this.SwapSize = 0;
                        this.Status = request.Status;
                        this.StartDate = request.StartDate;

                        db.Swaps.Add(this);
                        break;

                    case "charity-confirmed":
                        var declinedSwaps = db.Swaps.Where(s => s.Id != this.Id && s.SenderCollectionId == this.SenderCollectionId && s.Status == "charity").ToList();
                        foreach (var swap in declinedSwaps)
                        {
                            swap.SenderDisplaySwap = false;
                            db.Entry(this).State = EntityState.Modified;
                            await db.SaveChangesAsync();

                            var declineRequest = new SwapRequestViewModel
                            {
                                SwapId = swap.Id,
                                Status = "declined"
                            };
                            await swap.ProcessAsync(userId, declineRequest, db);
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

                    case "canceled":
                        if (userId == this.SenderId)
                        {
                            this.SenderDisplaySwap = false;
                        }
                        else if (userId == this.ReceiverId)
                        {
                            this.ReceiverDisplaySwap = false;
                        }
                        await ReleaseItems(this, db);
                        this.Status = request.Status;
                        db.Entry(this).State = EntityState.Modified;
                        break;

                    case "declined":
                        await ReleaseItems(this, db);
                        this.Status = request.Status;
                        db.Entry(this).State = EntityState.Modified;
                        break;

                    default:
                        break;
                }

                await db.SaveChangesAsync();
                return new ProcessSwapResult { Succeeded = true, SuccessType = swapStatus };
            }
            catch (Exception ex)
            {
                return new ProcessSwapResult { Succeeded = false, Error = ex.Message };
            }
        }
        public async Task Confirm(string type, string userId, ApplicationDbContext db)
        {
            var isCharity = this.SenderRequestedItems == "[]" || this.ReceiverRequestedItems == "[]";
            var userType = String.Empty;
            if (userId == this.SenderId)
            {
                userType = "sender";

                if (type == "sent")
                {
                    this.SenderConfirmSent = true;
                    this.SenderConfirmReceived = isCharity ? true : this.SenderConfirmReceived;
                    this.ReceiverConfirmSent = isCharity ? true : this.ReceiverConfirmSent;
                    this.SenderFeedbackSent = isCharity ? true : this.SenderFeedbackSent;

                    if (isCharity)
                    {
                        await ReleaseItems(this, db);
                        db.UserCollections.Find(this.SenderCollectionId).Delete(db);
                    }
                }
                else if (type == "received")
                {
                    this.SenderConfirmReceived = true;
                }
            }
            else if (userId == this.ReceiverId)
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
        private async Task<bool> ReleaseItems(Swap swap, ApplicationDbContext db)
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
                await db.SaveChangesAsync();
            }

            db.HeldItems.RemoveRange(heldItems);
            await db.SaveChangesAsync();

            return true;
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
            var pendingSwaps = db.Swaps
                    .Include("Collection")
                    .Include("Sender")
                    .Include("Receiver")
                    .Where(swap => swap.Id != this.Id && (swap.Sender.Id == userId || swap.Receiver.Id == userId) && (swap.Status == "offered" || swap.Status == "accepted") && swap.Collection.Id == this.CollectionId)
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
                    var sendersItems = JsonConvert.DeserializeObject<List<int>>(this.SenderCollection.ItemCountJSON);
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
                        var sendersItems = JsonConvert.DeserializeObject<List<int>>(this.SenderCollection.ItemCountJSON);
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
                        var receiversItems = JsonConvert.DeserializeObject<List<int>>(this.ReceiverCollection.ItemCountJSON);
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
                        var sendersItems = JsonConvert.DeserializeObject<List<int>>(this.SenderCollection.ItemCountJSON);
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
                        var receiversItems = JsonConvert.DeserializeObject<List<int>>(this.ReceiverCollection.ItemCountJSON);
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

            var swap = db.Swaps.Find(this.SwapId);

            this.SenderId = userId;
            this.ReceiverId = swap.SenderId == userId ? swap.ReceiverId : swap.SenderId;

            if (swap.SenderId == userId)
            {
                swap.SenderFeedbackSent = true;
            } 
            else if (swap.ReceiverId == userId)
            {
                swap.ReceiverFeedbackSent = true;
            }

            swap.Status = swap.SenderFeedbackSent && swap.ReceiverFeedbackSent ? "completed" : swap.Status;
            db.Entry(swap).State = EntityState.Modified;

            db.Feedbacks.Add(this);
            db.SaveChanges();

            return this;
        }
    }
}