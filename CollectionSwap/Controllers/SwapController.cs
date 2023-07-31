using CollectionSwap.Models;
using Microsoft.AspNet.Identity;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.UI.WebControls;

namespace CollectionSwap.Controllers
{
    public class PotentialSwap
    {
        public ApplicationUser User { get; set; }
        public Collection Collection { get; set; }
        public UserCollection UserCollection { get; set; }
        public List<int> MissingItems { get; set; }
        public List<int> DuplicateItems { get; set; }
    }

    public class MatchingSwap
    {
        public ApplicationUser Sender { get; set; }
        public ApplicationUser Receiver { get; set; }
        public int CollectionId { get; set; }
        public int UserCollectionId { get; set; }
        public List<int> SenderItemIds { get; set; }
        public List<int> ReceiverItemIds { get; set; }
    }

    public class SwapController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        public ActionResult Index()
        {
            var currentUserId = User.Identity.GetUserId();
            FindSwapsViewModel model = new FindSwapsViewModel
            {
                Users = db.Users.ToList(),
                Collections = db.Collections.ToList(),
                UserCollections = db.UserCollections.Where(uc => uc.User.Id == currentUserId).ToList(),
                OfferedSwaps = db.Swaps.Where(swap => swap.Receiver.Id == currentUserId && swap.Status == "offered").ToList(),
                AcceptedSwaps = db.Swaps.Where(swap => swap.Sender.Id == currentUserId && swap.Status == "accepted").ToList()
            };
            return View(model);
        }

        [Authorize]
        public ActionResult UserCollection(int? id)
        {
            var currentUserId = User.Identity.GetUserId();

            UserCollection selectedCollection = db.UserCollections.Find(id);

            FindSwapsViewModel model = new FindSwapsViewModel
            {
                Users = db.Users.ToList(),
                Collections = db.Collections.ToList(),
                UserCollections = db.UserCollections.Where(uc => uc.User.Id == currentUserId).ToList(),
                OfferedSwaps = db.Swaps.Where(swap => swap.Receiver.Id == currentUserId && swap.Status == "offered").ToList(),
                AcceptedSwaps = db.Swaps.Where(swap => swap.Sender.Id == currentUserId && swap.Status == "accepted").ToList()
            };

            if (id == null)
            {
                ViewBag.SelectedCollection = selectedCollection;
                return View(model);
            }

            var swappers = db.Users.ToList();
            List<PotentialSwap> swapList = new List<PotentialSwap>();
            foreach (var swapper in swappers)
            {
                List<int> missingItems = new List<int>();
                List<int> duplicateItems = new List<int>();

                List<UserCollection> userCollections = db.UserCollections.Where(uc => uc.User.Id == swapper.Id && uc.CollectionId == selectedCollection.CollectionId).ToList();

                foreach (var userCollection in userCollections)
                {
                    List<int> items = JsonConvert.DeserializeObject<List<int>>(userCollection.ItemCountJSON);

                    missingItems = items.Select((value, index) => new { value, index })
                                              .Where(item => item.value == 0)
                                              .Select(item => item.index)
                                              .ToList();

                    duplicateItems = items.SelectMany((value, index) => Enumerable.Repeat(index, Math.Max(value - 1, 0))
                                                .Where(i => value > 1))
                                                .ToList();

                    PotentialSwap newSwap = new PotentialSwap
                    {
                        User = swapper,
                        Collection = db.Collections.Find(selectedCollection.CollectionId),
                        UserCollection = userCollection,
                        MissingItems = missingItems,
                        DuplicateItems = duplicateItems
                    };
                    swapList.Add(newSwap);
                }

            
            }

            var matchingSwaps = FindMatchingSwaps(selectedCollection, swapList);

            ViewBag.MatchingSwaps = matchingSwaps;
            ViewBag.SelectedCollection = selectedCollection;

            return View(model);
        }

        private List<MatchingSwap> FindMatchingSwaps(UserCollection selectedCollection, List<PotentialSwap> potentialSwapList)
        {
            var currentUserId = User.Identity.GetUserId();

            PotentialSwap currentUserSwap = potentialSwapList.Where(swap => swap.User.Id == currentUserId && swap.UserCollection == selectedCollection).FirstOrDefault();

            List<MatchingSwap> matchingSwaps = new List<MatchingSwap>();

            if (currentUserSwap == null)
            {
                return new List<MatchingSwap>(); // User not found.
            }


            foreach (var potentialSwap in potentialSwapList)
            {
                // Find all pending swaps for the current user
                var pendingSwaps = db.Swaps
                    .Where(swap => (swap.Status == "offered" || swap.Status == "accepted") && swap.Sender.Id == currentUserId || swap.Receiver.Id == currentUserId)
                    .Select(swap => new { swap.SenderUserCollectionId, swap.ReceiverUserCollectionId })
                    .ToList();

                // Skip over this potentialSwap if it belongs to the current user or 
                // if the current user already has a pending swap matching this potentialSwap
                if (potentialSwap == currentUserSwap) { continue; }
                else if (pendingSwaps.Any(swap => (swap.SenderUserCollectionId == selectedCollection.Id && swap.ReceiverUserCollectionId == potentialSwap.UserCollection.Id) || (swap.SenderUserCollectionId == potentialSwap.UserCollection.Id && swap.ReceiverUserCollectionId == selectedCollection.Id))) { continue; }

                var currentUserNeededItems = currentUserSwap.MissingItems.Intersect(potentialSwap.DuplicateItems).ToList();
                var otherUserNeededItems = currentUserSwap.DuplicateItems.Intersect(potentialSwap.MissingItems).ToList();

                if (currentUserNeededItems.Any() && otherUserNeededItems.Any())
                {
                    MatchingSwap matchingSwap = new MatchingSwap
                    {
                        Sender = db.Users.Find(currentUserId),
                        Receiver = potentialSwap.User,
                        CollectionId = potentialSwap.Collection.Id,
                        UserCollectionId = potentialSwap.UserCollection.Id,
                        SenderItemIds = currentUserNeededItems,
                        ReceiverItemIds = otherUserNeededItems
                    };

                    matchingSwaps.Add(matchingSwap);
                }
            }

            matchingSwaps = matchingSwaps
            .OrderByDescending(swap => Math.Min(swap.SenderItemIds.Count(), swap.ReceiverItemIds.Count()))
            .ThenByDescending(swap => swap.SenderItemIds.Count())
            .ToList();

            return matchingSwaps;
        }

        [Authorize]
        public ActionResult HandleSwap(Swap swap)
        {
            if (ModelState.IsValid)
            {
                var user = db.Users.ToList();
                switch (swap.Status)
                {
                    case "offered":
                        db.Swaps.Add(swap);
                        db.SaveChanges();

                        Response.Cookies["swapSuccessMessage"].Value = $"Offer made, waiting for {swap.Receiver.UserName} to accept.";
                        break;
                    case "accepted":
                        var receiverItems = db.UserCollections.Find(swap.ReceiverUserCollectionId);
                        
                        HoldItems(swap.ReceiverItemIdsJSON, receiverItems);
                        db.Entry(swap).State = EntityState.Modified;                        
                        db.SaveChanges();

                        Response.Cookies["swapSuccessMessage"].Value = $"Offer accepted, waiting for {swap.Sender.UserName} to confirm.";
                        break;
                    case "confirmed":
                        break;
                    default:
                        break;
                }
                return Json(new { reloadPage = true });
            }

            return Json(new { reloadPage = false });
        }

        private void HoldItems(string itemListJSON, UserCollection userCollection)
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
                UserCollection = userCollection
            };

            db.Entry(userCollection).State = EntityState.Modified;
            db.HeldItems.Add(heldItems);
            db.SaveChanges();
        }

        private void ReleaseItems()
        {

        }

        private void SwapItems()
        {

        }

        [Authorize]
        public ActionResult Offers()
        {
            //string acceptedSuccess = Request.Cookies["acceptedSuccess"]?.Value;
            //acceptedSuccess = acceptedSuccess != null ? HttpUtility.UrlDecode(acceptedSuccess) : null;
            var currentUserId = User.Identity.GetUserId();
            FindSwapsViewModel model = new FindSwapsViewModel
            {
                Users = db.Users.ToList(),
                Collections = db.Collections.ToList(),
                UserCollections = db.UserCollections.Where(uc => uc.User.Id == currentUserId).ToList(),
                OfferedSwaps = db.Swaps.Where(swap => swap.Receiver.Id == currentUserId && swap.Status == "offered").ToList(),
                AcceptedSwaps = db.Swaps.Where(swap => swap.Sender.Id == currentUserId && swap.Status == "accepted").ToList()
            };

            //ViewBag.AcceptedSuccess = acceptedSuccess;
            return View(model);
        }
    }
}