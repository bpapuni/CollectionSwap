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
        public ValidateResult Validation { get; set; }
    }

    public class YourSwapsViewModel
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
        public string SenderRequestedItems { get; set; }
        [Required]
        public bool SenderConfirmSent { get; set; }
        [Required]
        public bool SenderConfirmReceived { get; set; }
        public virtual Feedback SenderFeedback { get; set; }
        public bool SenderDisplaySwap { get; set; } = true;
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
        public virtual Collection Collection { get; set; }
        public virtual ApplicationUser Sender { get; set; }
        public virtual UserCollection SenderCollection { get; set; }
        public virtual ApplicationUser Receiver { get; set; }
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
                var isCharity = request.Status != "requested" && (this.SenderRequestedItems == null || this.SenderRequestedItems == "[]" || this.ReceiverRequestedItems == null || this.ReceiverRequestedItems == "[]");
                var swapStatus = isCharity ? $"charity-{request.Status}" : request.Status;
                switch (swapStatus)
                {
                    case "charity-requested":
                        senderItemCount = JsonConvert.DeserializeObject<List<int>>(db.UserCollections.Where(uc => uc.Id == request.SenderUserCollectionId).FirstOrDefault().ItemCountJSON);
                        senderItemCount = senderItemCount
                                            .SelectMany((value, index) => Enumerable.Repeat(index, value))
                                            .ToList();

                        this.Collection = db.Collections.Find(request.CollectionId);
                        this.SenderCollection = db.UserCollections.Find(request.SenderUserCollectionId);
                        this.ReceiverCollection = db.UserCollections.Find(request.ReceiverUserCollectionId);
                        this.Sender = db.Users.Find(request.ReceiverId);                        // Sender and receiver are switched for donated items
                        this.Receiver = db.Users.Find(userId);
                        this.SenderRequestedItems = JsonConvert.SerializeObject(senderItemCount);   // Snapshot of the items the sender has on offer 
                        this.ReceiverRequestedItems = JsonConvert.SerializeObject(new List<int>());
                        this.SwapSize = 0;
                        this.Status = request.Status;
                        this.StartDate = request.StartDate;

                        db.Swaps.Add(this);
                        break;
                    case "charity-confirmed":
                        var declinedSwaps = db.Swaps.Where(s => s.Id != this.Id && s.SenderCollection.Id == this.SenderCollection.Id && s.Status == "charity").ToList();

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

                        HoldItems(this.SenderRequestedItems, this.SenderCollection, this, db);
                        break;
                    case "requested":
                        this.Collection = db.Collections.Find(request.CollectionId);
                        this.SenderCollection = db.UserCollections.Find(request.SenderUserCollectionId);
                        this.ReceiverCollection = db.UserCollections.Find(request.ReceiverUserCollectionId);
                        this.Sender = db.Users.Find(userId);
                        this.Receiver = db.Users.Find(request.ReceiverId);
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

                        HoldItems(this.ReceiverRequestedItems, this.ReceiverCollection, this, db);
                        break;
                    case "confirmed":
                        this.ReceiverRequestedItems = request.RequestedItems;
                        this.Status = request.Status;
                        db.Entry(this).State = EntityState.Modified;

                        HoldItems(this.SenderRequestedItems, this.SenderCollection, this, db);
                        break;
                    case "charity-canceled":
                    case "canceled":
                        if (this.Status == "requested")
                        {
                            this.SenderDisplaySwap = false;
                            this.ReceiverDisplaySwap = false;
                        }
                        else if (userId == this.Sender.Id)
                        {
                            this.SenderDisplaySwap = false;
                        }
                        else if (userId == this.Receiver.Id)
                        {
                            this.ReceiverDisplaySwap = false;
                        }
                        ReleaseItems(this, db);
                        this.Status = request.Status;
                        db.Entry(this).State = EntityState.Modified;
                        break;
                    case "charity-declined":
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
            if (userId == this.Sender.Id)
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
                        db.UserCollections.Find(this.SenderCollection.Id).Delete(db);
                    }
                }
                // If user has clicked the received items checkbox
                else if (type == "received")
                {
                    this.SenderConfirmReceived = true;
                }
            }
            else if (userId == this.Receiver.Id)
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
                    heldItems = db.HeldItems
                        .Where(hi => hi.Swap.Id == swap.Id &&
                        hi.UserCollection.UserId != swap.Sender.Id)
                        .FirstOrDefault();

                    // Define the User Collection that will be getting updated as the senders
                    userCollection = db.UserCollections.Find(swap.SenderCollection.Id);
                    break;

                case "receiver":
                    // Find the items that DO NOT belong to to receiver
                    heldItems = db.HeldItems
                        .Where(hi => hi.Swap.Id == swap.Id &&
                        hi.UserCollection.UserId != swap.Receiver.Id)
                        .FirstOrDefault();

                    // Define the User Collection that will be getting updated as the receivers
                    userCollection = db.UserCollections.Find(swap.ReceiverCollection.Id);
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
        public string StatusForUser(string userId)
        {
            var isCharity = this.ReceiverRequestedItems == "[]";
            var status = isCharity && (this.Status == "requested" || this.Status == "accepted") ? $"charity-{this.Status}" : this.Status;
            var senderPseudoCompleted = this.Status == "confirmed" && this.SenderConfirmSent && this.SenderConfirmReceived;
            var receiverPseudoCompleted = this.Status == "confirmed" && this.ReceiverConfirmSent && this.ReceiverConfirmReceived;
            status = (this.Sender.Id == userId && senderPseudoCompleted) || (this.Receiver.Id == userId && receiverPseudoCompleted) ? "pseudo-completed" : status;
            
            return status;
        }
        public string StatusColor(string userId)
        {
            var itemsSent = (userId == this.Sender.Id && this.SenderConfirmSent) || (userId == this.Receiver.Id && this.ReceiverConfirmSent);
            var itemsReceived = (userId == this.Sender.Id && this.SenderConfirmReceived) || (userId == this.Receiver.Id && this.ReceiverConfirmReceived);
            var feedbackProvided = (userId == this.Sender.Id && this.SenderFeedback != null) || (userId == this.Receiver.Id && this.ReceiverFeedback != null);
            var statusColor = 
                this.Status == "requested" ? userId == this.Sender.Id ? "blue" : "orange" :
                this.Status == "accepted" ? userId == this.Sender.Id ? "orange" : "blue" :
                this.Status == "confirmed" && itemsSent && itemsReceived && feedbackProvided ? "green" :
                this.Status == "confirmed" && itemsSent && !itemsReceived ? "blue" :
                this.Status == "confirmed" ? "orange" :
                this.Status == "completed" ? "green" : "red";

            return statusColor;
        }
        public string StatusMessage(string userId)
        {
            var itemsSent = (userId == this.Sender.Id && this.SenderConfirmSent) || (userId == this.Receiver.Id && this.ReceiverConfirmSent);
            var itemsReceived = (userId == this.Sender.Id && this.SenderConfirmReceived) || (userId == this.Receiver.Id && this.ReceiverConfirmReceived);
            var feedbackProvided = (userId == this.Sender.Id && this.SenderFeedback != null) || (userId == this.Receiver.Id && this.ReceiverFeedback != null);
            var hasExpired = (DateTime.Now - this.StartDate).Days > 14;
            switch (this.Status)
            {
                case "charity-requested":
                    if (userId == this.Sender.Id)
                        return "Request Pending";
                    else
                        return $"Waiting for {this.Sender.UserName}";
                case "canceled":
                case "charity-canceled":
                    return "Canceled";
                case "declined":
                case "charity-declined":
                    return "Declined";
                case "charity-confirmed":
                    if (feedbackProvided)
                        return "Complete";
                    else if (itemsSent && itemsReceived)
                        return "Awaiting Feedback";
                    else if (itemsSent)
                        return "Waiting to receive items";
                    else
                        return "Needs Mailing";
                case "requested":
                    if (userId == this.Sender.Id)
                        return $"Waiting for {this.Receiver.UserName}";
                    else
                        return "Request Pending";
                case "accepted":
                    if (userId == this.Sender.Id)
                        return "Pending Confirmation";
                    else
                        return $"Waiting for {this.Sender.UserName}";
                case "confirmed":
                    if (feedbackProvided)
                        return "Complete";
                    else if ((itemsSent && itemsReceived) || hasExpired)
                        return "Awaiting Feedback";
                    else if (itemsSent)
                        return "Waiting to receive items";
                    else
                        return "Needs Mailing";
                default:
                    return "Complete";
            }
        }
        public int ProgressIndex(string userId)
        {
            var itemsSent = (userId == this.Sender.Id && this.SenderConfirmSent) || (userId == this.Receiver.Id && this.ReceiverConfirmSent);
            var itemsReceived = (userId == this.Sender.Id && this.SenderConfirmReceived) || (userId == this.Receiver.Id && this.ReceiverConfirmReceived);
            var otherUserSent = (userId == this.Sender.Id && this.ReceiverConfirmSent) || (userId == this.Receiver.Id && this.SenderConfirmSent);
            var progIndex =
                this.Status == "requested" ? 0 :
                this.Status == "accepted" ? 1 :
                this.Status == "confirmed" && itemsSent && itemsReceived ? 4 :
                this.Status == "confirmed" && otherUserSent ? 3 :
                this.Status == "confirmed" ? 2 :
                this.Status == "completed" ||
                this.Status == "canceled" ||
                this.Status == "declined" ? 4 : -1;

            return progIndex;
        }
        public string ProgressTooltip(string userId, int progIndex)
        {
            var status = StatusForUser(userId);

            switch (status)
            {
                case "charity-requested":
                    if (userId == this.Sender.Id)
                        return $"{this.Receiver.UserName} has requested your items.";
                    else
                        return $"You've requested these items, waiting for {this.Sender.UserName} to accept or decline.";
                case "charity-confirmed":
                    if (userId == this.Sender.Id)
                        return "You confirmed this swap, please follow the provided mailing instructions.";
                    else
                        return $"{this.Sender.UserName} has accepted your request.";
                case "charity-canceled":
                    return $"{this.Receiver.UserName} canceled their request.";
                case "charity-declined":
                    return $"{this.Sender.UserName} has donated the items to another user.";
                case "requested":
                    if (userId == this.Sender.Id)
                        return $"You requested this swap, waiting for {this.Receiver.UserName} to accept or decline.";
                    else
                        return $"{this.Sender.UserName} has requested this swap from you, waiting for you to accept or decline.";
                case "accepted":
                    if (userId == this.Sender.Id)
                        return $"{this.Receiver.UserName} accepted this swap, waiting for you to confirm.";
                    else
                        return $"You've accepted this swap, waiting for {this.Sender.UserName} to confirm.";
                case "confirmed":
                    if (userId == this.Sender.Id)
                    {
                        if (this.ReceiverConfirmSent && !this.SenderConfirmSent)
                            return $"{this.Receiver.UserName} has sent your items. Please follow the provided mailing instructions.";
                        else if (this.ReceiverConfirmSent)
                            return $"{this.Receiver.UserName} has sent your items. Please mark them as 'received' as soon as they arrive.";
                        else if (this.SenderConfirmSent)
                            return $"{this.Receiver.UserName} has yet to send your items. Please mark them as 'received' as soon as they arrive.";
                        else
                            return "You confirmed this swap, please follow the provided mailing instructions.";
                    }
                    else
                    {
                        if (this.SenderConfirmSent && !this.ReceiverConfirmSent)
                            return $"{this.Sender.UserName} has sent your items. Please follow the provided mailing instructions.";
                        else if (this.SenderConfirmSent)
                            return $"{this.Sender.UserName} has sent your items. Please mark them as 'received' as soon as they arrive.";
                        else if (this.ReceiverConfirmSent)
                            return $"{this.Sender.UserName} has yet to send your items. Please mark them as 'received' as soon as they arrive.";
                        else
                            return $"{this.Sender.UserName} confirmed this swap, please follow the provided mailing instructions.";
                    }
                case "pseudo-completed":
                    if (userId == this.Sender.Id)
                    {
                        if (this.SenderFeedback == null)
                            return $"Swap completed, please provide feedback about your swap with {this.Receiver.UserName}";
                        else
                            return "This swap has been completed.";
                    }
                    else
                    {
                        if (this.ReceiverFeedback == null)
                            return $"Swap completed, please provide feedback about your swap with {this.Sender.UserName}";
                        else
                            return "This swap has been completed.";
                    }
                case "completed":
                case "charity-completed":
                    return "This swap has been completed.";
                case "canceled":
                case "declined":
                    return $"This swap has been {status}.";
                default:
                    return String.Empty;
            }
        }
        public static List<Swap> Filter(string userId, string filter, ApplicationDbContext db)
        {
            var swaps = db.Swaps.Where(s => (s.Sender.Id == userId && s.SenderDisplaySwap) || (s.Receiver.Id == userId && s.ReceiverDisplaySwap)).ToList();
            var result = filter == "all" ? swaps : swaps.Where(s => s.Status == filter).ToList();

            if (filter == "confirmed")
            {
                // When a user has sent and received the items involved in their transaction they're essentially "completed" but the swap.Status does not reflect that
                // until both users are done. We do still want to filter these swaps out though, so that's what this is doing.
                result.RemoveAll(s => (s.Sender.Id == userId && s.SenderConfirmSent && s.SenderConfirmReceived) || (s.Receiver.Id == userId && s.ReceiverConfirmSent && s.ReceiverConfirmReceived));
            }
            else if (filter == "completed")
            {
                // Add in 'confirmed' swaps where the user has sent and received the items involved in their transaction
                var pseudoCompletedSwaps = swaps.Where(s => s.Status == "confirmed" && ((s.Sender.Id == userId && s.SenderConfirmSent && s.SenderConfirmReceived) || (s.Receiver.Id == userId && s.ReceiverConfirmSent && s.ReceiverConfirmReceived)));
                result.AddRange(pseudoCompletedSwaps);
            }

            result = result
                .OrderBy(s => s.Status == "requested" ? 0 : 1)
                .ThenBy(s => s.Status == "accepted" ? 0 : 1)
                // Of the confirmed swaps, show first the swaps where the user hasnt sent their items yet
                .ThenBy(s => s.Status == "confirmed" && ((s.Sender.Id == userId && s.SenderConfirmSent == false) || (s.Receiver.Id == userId && s.ReceiverConfirmSent == false)) ? 0 : 1)
                // Of the confirmed swaps, then show the swaps where the OTHER user hasnt sent their items yet
                .ThenBy(s => s.Status == "confirmed" && ((s.Sender.Id == userId && s.ReceiverConfirmSent == false) || (s.Receiver.Id == userId && s.SenderConfirmSent == false)) ? 0 : 1)
                // Of the confirmed swaps, then show the swaps where the user hasnt received their items yet
                .ThenBy(s => s.Status == "confirmed" && ((s.Sender.Id == userId && s.SenderConfirmReceived == false) || (s.Receiver.Id == userId && s.ReceiverConfirmReceived == false)) ? 0 : 1)
                // Then show confirmed swaps that are 'pseudo-completed', that is, the exchange is done but the user hasn't provided feedback yet
                .ThenBy(s => s.StatusForUser(userId) == "pseudo-completed" && ((s.Sender.Id == userId && s.SenderFeedback == null) || (s.Receiver.Id == userId && s.ReceiverFeedback == null)) ? 0 : 1)
                // Then show confirmed swaps that are 'pseudo-completed', but only awaiting other users feedback OR are complete
                .ThenBy(s => (s.StatusForUser(userId) == "pseudo-completed" && ((s.Sender.Id == userId && s.SenderFeedback != null) || (s.Receiver.Id == userId && s.ReceiverFeedback != null)) || s.Status == "completed") ? 0 : 1)
                .ThenBy(s => s.Status == "declined" ? 0 : 1)
                .ThenBy(s => s.Status == "canceled" ? 0 : 1)
                .ThenByDescending(s => s.StartDate)
                .ToList();


            return result;
        }
        public ValidateResult Validate(string userId, ApplicationDbContext db)
        {
            var result = new ValidateResult();
            var senderRequestedItems = JsonConvert.DeserializeObject<List<int>>(this.SenderRequestedItems);     // Items requested from (not by) the sender
            var receiverRequestedItems = JsonConvert.DeserializeObject<List<int>>(this.ReceiverRequestedItems); // Items requested from (not by) the receiver
            // Find all swaps (not including this swap) where this swaps user collection id is involved            
            var pendingSwaps = db.Swaps
                .Where(swap => (swap.Sender.Id == userId || swap.Receiver.Id == userId) && 
                    this.Id != swap.Id && 
                    swap.ReceiverRequestedItems != "[]" && 
                    this.ReceiverRequestedItems != "[]" &&
                        (this.SenderCollection.Id == swap.SenderCollection.Id || 
                        this.SenderCollection.Id == swap.ReceiverCollection.Id || 
                        this.ReceiverCollection.Id == swap.SenderCollection.Id || 
                        this.ReceiverCollection.Id == swap.ReceiverCollection.Id) && 
                    (swap.Status == "requested" || swap.Status == "accepted"))
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
            else if (this.Status == "requested") // Check if receiver has items available to accept swap
            {
                if (userId == this.Sender.Id)
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
                else if (userId == this.Receiver.Id)
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
                if (userId == this.Sender.Id)
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
                else if (userId == this.Receiver.Id)
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
                    "Items in good condition",
                    "Generous swapper",
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
                    "Swap took a long time to complete",
                    "Items in poor condition",
                    "Expected items were missing",
                    "Would not swap with again",
                    "User came to my address",
                    "User shared personal information",
                    "User tried to communicate privately"
                };
            }
        }
        public Feedback Create(string userId, ApplicationDbContext db)
        {
            var comments = JsonConvert.DeserializeObject<List<string>>(this.Comments);
            var feedback = db.Feedbacks.Find(this.Id) ?? new Feedback();

            // Guard clause that prevents the user from submitting a comment not contained within our feedback options
            foreach (var comment in comments) 
            { 
                if (!PositiveComments.Contains(comment) && !NegativeComments.Contains(comment))
                {
                    return null;
                }
            }

            // If this is new feedback
            if (feedback.Id == 0)
            {
                this.DatePlaced = DateTime.UtcNow;
                db.Feedbacks.Add(this);

                // Find the swap this feedback is for
                var swap = db.Swaps.Find(this.SwapId);
                // Determine if the swap was a donation
                var isCharity = swap.ReceiverRequestedItems == "[]";

                this.Sender = db.Users.Find(userId);
                this.Receiver = db.Users.Find(userId == swap.Sender.Id ? swap.Receiver.Id : swap.Sender.Id);

                if (userId == swap.Sender.Id)
                {
                    swap.SenderFeedback = this;
                }
                else if (userId == swap.Receiver.Id)
                {
                    swap.ReceiverFeedback = this;
                }

                // If both sender and receiver have sent feedback OR if the swaps charity and the receiver has sent feedback, set status to completed
                swap.Status = (swap.SenderFeedback != null && swap.ReceiverFeedback != null) || (isCharity && swap.ReceiverFeedback != null) ? "completed" : swap.Status;
                db.Entry(swap).State = EntityState.Modified;
            }
            // Else its updating feedback
            else
            {
                feedback.Rating = this.Rating;
                feedback.Comments = this.Comments;
                db.Entry(feedback).State = EntityState.Modified;
            }

            db.SaveChanges();

            return this;
        }
    }
}