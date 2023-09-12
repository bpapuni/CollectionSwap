﻿using CollectionSwap.Models;
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
        public Address Address { get; set; }
        public double Rating { get; set; }
        public ValidateResult Validation { get; set; }
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

            //if (this.SenderConfirmSent &&
            //    this.SenderConfirmReceived &&
            //    this.ReceiverConfirmSent &&
            //    this.ReceiverConfirmReceived)
            //{
            //    this.Status = "completed";
            //}

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
                    var sendersPendingSwapItems = pendingSwaps.SelectMany(swap => JsonConvert.DeserializeObject<List<int>>(swap.ReceiverRequestedItems)).ToList();

                    // If any items from 'this' swap are within the created list they are duplicate request items
                    if (sendersPendingSwapItems.Contains(item))
                    {
                        result.DuplicateRequestItems.Add(item);
                    }
                }

                // Checks if sender has enough copies of an item
                foreach (var item in senderRequestedItems) // For each item in items offered to receiver
                {
                    // Creates a list of all the items offered by the sender in their other swaps
                    var sendersPendingSwapItems = pendingSwaps.SelectMany(swap => JsonConvert.DeserializeObject<List<int>>(swap.SenderRequestedItems)).ToList();
                    var sendersItems = JsonConvert.DeserializeObject<List<int>>(this.SenderCollection.ItemCountJSON);
                    // Calculates the number of times an item has been offered
                    var offeredItemCount = sendersPendingSwapItems.Count(i => i == item);

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
                        var sendersPendingSwapItems = pendingSwaps.SelectMany(swap => JsonConvert.DeserializeObject<List<int>>(swap.ReceiverRequestedItems)).ToList();

                        // If any items from 'this' swap are within the created list they are duplicate request items
                        if (sendersPendingSwapItems.Contains(item))
                        {
                            result.DuplicateRequestItems.Add(item);
                        }
                    }
                    
                    // Checks if sender has enough copies of an item
                    foreach (var item in senderRequestedItems) // For each item in items offered to receiver
                    {
                        // Creates a list of all the items offered by the sender in their other swaps
                        var sendersPendingSwapItems = pendingSwaps.SelectMany(swap => JsonConvert.DeserializeObject<List<int>>(swap.SenderRequestedItems)).ToList();
                        var sendersItems = JsonConvert.DeserializeObject<List<int>>(this.SenderCollection.ItemCountJSON);
                        // Calculates the number of times an item has been offered
                        var offeredItemCount = sendersPendingSwapItems.Count(i => i == item);

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
                //var sendersItems = JsonConvert.DeserializeObject<List<int>>(this.SenderCollection.ItemCountJSON);
                //foreach (var item in senderRequestedItems)
                //{
                //    if (sendersItems[item] <= 1)
                //    {
                //        result.LowInventoryItems.Add(item);
                //        result.IsValid = false;
                //    }
                //}
                if (userId == this.SenderId)
                {
                    // Checks if sender has already requested an item
                    foreach (var item in receiverRequestedItems)
                    {
                        // Creates a list of all the items requested by the sender in their other swaps
                        var sendersPendingSwapItems = pendingSwaps.SelectMany(swap => JsonConvert.DeserializeObject<List<int>>(swap.ReceiverRequestedItems)).ToList();

                        // If any items from 'this' swap are within the created list they are duplicate request items
                        if (sendersPendingSwapItems.Contains(item))
                        {
                            result.DuplicateRequestItems.Add(item);
                        }
                    }

                    // Checks if sender has enough copies of an item
                    foreach (var item in senderRequestedItems) // For each item in items offered to receiver
                    {
                        // Creates a list of all the items offered by the sender in their other swaps
                        var sendersPendingSwapItems = pendingSwaps.SelectMany(swap => JsonConvert.DeserializeObject<List<int>>(swap.SenderRequestedItems)).ToList();
                        var sendersItems = JsonConvert.DeserializeObject<List<int>>(this.SenderCollection.ItemCountJSON);
                        // Calculates the number of times an item has been offered
                        var offeredItemCount = sendersPendingSwapItems.Count(i => i == item);

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
        public Dictionary<string, string> FindDuplicates(string userId, List<Swap> usersSwaps)
        {
            var offeredSwaps = usersSwaps.Where(swap => swap.Status == "offered" && swap.CollectionId == CollectionId).ToList();
            var acceptedSwaps = usersSwaps.Where(swap => swap.Status == "accepted" && swap.CollectionId == CollectionId).ToList();

            var senderItemList = this.Sender.Id == userId ? JsonConvert.DeserializeObject<List<int>>(this.SenderCollection.ItemCountJSON) : null;
            var receiverItemList = this.Receiver.Id == userId ? JsonConvert.DeserializeObject<List<int>>(this.ReceiverCollection.ItemCountJSON) : null;

            var requestedItems = new List<int>();
            var offeredItems = new List<int>();
            var acceptedItems = new List<int>();
            var duplicateRequestedItems = new List<int>();
            var duplicateOfferedItems = new List<int>();
            var duplicateAcceptedItems = new List<int>();

            foreach (var swap in offeredSwaps)
            {
                var senderItems = JsonConvert.DeserializeObject<List<int>>(swap.SenderRequestedItems);
                var receiverItems = JsonConvert.DeserializeObject<List<int>>(swap.ReceiverRequestedItems);

                // Create list of all items that the user has offered to others
                var items = Sender.Id == userId ? senderItems : receiverItems; 
                offeredItems.AddRange(items);

                // Create list of all items that the user has requested
                items = Sender.Id == userId ? receiverItems : senderItems;
                requestedItems.AddRange(items);
            }

            // Create new array to track the number of each item the user has currently offered
            var offeredItemsCount = this.Sender.Id == userId ? new int[senderItemList.Count] : new int[receiverItemList.Count];
            foreach (var item in offeredItems)
            {
                offeredItemsCount[item]++;
            }

            foreach (var swap in acceptedSwaps)
            {
                var senderItems = JsonConvert.DeserializeObject<List<int>>(swap.SenderRequestedItems);
                var receiverItems = JsonConvert.DeserializeObject<List<int>>(swap.ReceiverRequestedItems);

                // Find all items that other user has accepted an offer for
                var items = Sender.Id == userId ? senderItems : receiverItems;
                acceptedItems.AddRange(items);

                // Find all items that the user has requested
                items = Sender.Id == userId ? receiverItems : senderItems;
                requestedItems.AddRange(items);
            }

            // Create new array to track the number of each item the user has currently accepted
            var acceptedItemsCount = this.Sender.Id == userId ? new int[senderItemList.Count] : new int[receiverItemList.Count];
            foreach (var item in acceptedItems)
            {
                acceptedItemsCount[item]++;
            }

            // Return duplicate items that YOU have requested
            if (Status == "swap")
            {
                var items = JsonConvert.DeserializeObject<List<int>>(this.SenderRequestedItems);
                for (int i = 0; i < items.Count; i++)
                {
                    if (requestedItems.Count(item => item == items[i]) > 1)
                    {
                        duplicateRequestedItems.Add(items[i]);
                    }
                }
            }

            // Return duplicate items that YOU have requested
            if (Status == "offered")
            {
                if (userId == this.ReceiverId)
                {
                    var items = JsonConvert.DeserializeObject<List<int>>(this.ReceiverRequestedItems);
                    for (int i = 0; i < items.Count; i++)
                    {
                        if (offeredItems.Count(item => item == items[i]) > 1)
                        {
                            duplicateOfferedItems.Add(items[i]);
                        }
                    }
                }
                else if (userId == this.SenderId)
                {
                    var items = JsonConvert.DeserializeObject<List<int>>(this.ReceiverRequestedItems);
                    for (int i = 0; i < items.Count; i++)
                    {
                        if (requestedItems.Count(item => item == items[i]) > 1)
                        {
                            duplicateRequestedItems.Add(items[i]);
                        }
                    }
                }
            }

            // Return duplicate items that YOU have requested
            if (Status == "accepted")
            {
                if (userId == this.ReceiverId)
                { 
                    var items = JsonConvert.DeserializeObject<List<int>>(this.SenderRequestedItems);
                    for (int i = 0; i < items.Count; i++)
                    {
                        if (acceptedItems.Count(item => item == items[i]) > 1)
                        {
                            duplicateAcceptedItems.Add(items[i]);
                        }
                    }
                }
                else if (userId == this.SenderId)
                {
                    var items = JsonConvert.DeserializeObject<List<int>>(this.ReceiverRequestedItems);
                    for (int i = 0; i < items.Count; i++)
                    {
                        if (requestedItems.Count(item => item == items[i]) > 1)
                        {
                            duplicateRequestedItems.Add(items[i]);
                        }
                    }

                    items = JsonConvert.DeserializeObject<List<int>>(this.SenderRequestedItems);
                    for (int i = 0; i < items.Count; i++)
                    {
                        if (acceptedItems.Count(item => item == items[i]) > 1)
                        {
                            duplicateAcceptedItems.Add(items[i]);
                        }
                    }
                }
            }

            return new Dictionary<string, string> {
                { "requestedItems", JsonConvert.SerializeObject(duplicateRequestedItems) },
                { "offeredItems", JsonConvert.SerializeObject(duplicateOfferedItems) },
                { "acceptedItems", JsonConvert.SerializeObject(duplicateAcceptedItems) }
            };

            //for (int i = 0; i < yourItemList.Count; i++)
            //{
            //    // check if you have enough of each item to swap
            //    if (yourItemList[i] > 0 && offeredItemsCount[i] > 0 && yourItemList[i] - offeredItemsCount[i] < 1)
            //    {
            //        duplicateOfferedItems.Add(i);
            //        return JsonConvert.SerializeObject(duplicateOfferedItems);
            //    }
            //    // check if you have enough of each item to swap
            //    if (yourItemList[i] > 0 && acceptedItemsCount[i] > 0 && yourItemList[i] - acceptedItemsCount[i] < 1 && userId == this.SenderId)
            //    {
            //        duplicateAcceptedItems.Add(i);
            //        return JsonConvert.SerializeObject(duplicateAcceptedItems);
            //    }
            //}
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