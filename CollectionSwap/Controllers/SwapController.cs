using CollectionSwap.Models;
using Microsoft.AspNet.Identity;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.UI.WebControls;

namespace CollectionSwap.Controllers
{
    public class PotentialSwap
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public int CollectionId { get; set; }
        public int UserCollectionId { get; set; }
        public List<int> MissingItems { get; set; }
        public List<int> DuplicateItems { get; set; }
    }

    public class MatchingSwap
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public int CollectionId { get; set; }
        public int UserCollectionId { get; set; }
        public List<int> CurrentUserNeededItems { get; set; }
        public List<int> OtherUserNeededItems { get; set; }
    }

    public class SwapController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        public ActionResult Index()
        {
            var currentUserId = User.Identity.GetUserId();
            SwapViewModel model = new SwapViewModel
            {
                Collections = db.Collections.ToList(),
                UserCollections = db.UserCollections.Where(uc => uc.UserId == currentUserId).ToList()
            };
            return View(model);
        }

        [Authorize]
        public ActionResult UserCollection(int? id)
        {
            var currentUserId = User.Identity.GetUserId();
            
            UserCollection selectedCollection = db.UserCollections.Find(id);

            SwapViewModel model = new SwapViewModel
            {
                Collections = db.Collections.ToList(),
                UserCollections = db.UserCollections.Where(uc => uc.UserId == currentUserId).ToList()
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

                List<UserCollection> userCollections = db.UserCollections.Where(uc => uc.UserId == swapper.Id && uc.CollectionId == selectedCollection.CollectionId).ToList();

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
                        UserId = swapper.Id,
                        UserName = swapper.UserName,
                        CollectionId = selectedCollection.CollectionId,
                        UserCollectionId = userCollection.Id,
                        MissingItems = missingItems,
                        DuplicateItems = duplicateItems
                    };
                    swapList.Add(newSwap);
                }

            
            }

            var matchingSwaps = FindMatchingSwaps(selectedCollection.Id, swapList);

            ViewBag.MatchingSwaps = matchingSwaps;
            ViewBag.SelectedCollection = selectedCollection;

            return View(model);
        }

        private List<MatchingSwap> FindMatchingSwaps(int selectedCollectionId, List<PotentialSwap> potentialSwapList)
        {
            var currentUserId = User.Identity.GetUserId();

            PotentialSwap currentUserSwap = potentialSwapList.Where(swap => swap.UserId == currentUserId && swap.UserCollectionId == selectedCollectionId).FirstOrDefault();

            List<MatchingSwap> matchingSwaps = new List<MatchingSwap>();

            if (currentUserSwap == null)
            {
                return new List<MatchingSwap>(); // User not found.
            }


            foreach (var potentialSwap in potentialSwapList)
            {
                // Do not carry out the logic on the selectedCollection
                if (potentialSwap == currentUserSwap) { continue; }

                var currentUserNeededItems = currentUserSwap.MissingItems.Intersect(potentialSwap.DuplicateItems).ToList();
                var otherUserNeededItems = currentUserSwap.DuplicateItems.Intersect(potentialSwap.MissingItems).ToList();

                if (currentUserNeededItems.Any() && otherUserNeededItems.Any())
                {
                    MatchingSwap matchingSwap = new MatchingSwap
                    {
                        UserId = potentialSwap.UserId,
                        UserName = potentialSwap.UserName,
                        CollectionId = potentialSwap.CollectionId,
                        UserCollectionId = potentialSwap.UserCollectionId,
                        CurrentUserNeededItems = currentUserNeededItems,
                        OtherUserNeededItems = otherUserNeededItems
                    };

                    matchingSwaps.Add(matchingSwap);
                }
            }

            matchingSwaps = matchingSwaps
            .OrderByDescending(swap => Math.Min(swap.CurrentUserNeededItems.Count(), swap.OtherUserNeededItems.Count()))
            .ThenByDescending(swap => swap.CurrentUserNeededItems.Count())
            .ToList();

            return matchingSwaps;
        }
    }
}