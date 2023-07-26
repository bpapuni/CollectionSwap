using CollectionSwap.Models;
using Microsoft.AspNet.Identity;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace CollectionSwap.Controllers
{
    public class Swapper
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public Dictionary<int, List<int>> MissingItems { get; set; }
        public Dictionary<int, List<int>> DuplicateItems { get; set; }
    }

    public class SwapController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        public ActionResult Index()
        {
            return View();
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
            List<Swapper> swapperList = new List<Swapper>();
            foreach (var swapper in swappers)
            {
                Dictionary<int, List<int>> missingItems = new Dictionary<int, List<int>>();
                Dictionary<int, List<int>> duplicateItems = new Dictionary<int, List<int>>();

                List<UserCollection> userCollections = db.UserCollections.Where(uc => uc.UserId == swapper.Id && uc.CollectionId == selectedCollection.CollectionId).ToList();
                foreach (var userCollection in userCollections)
                {
                    List<int> items = JsonConvert.DeserializeObject<List<int>>(userCollection.ItemCountJSON);
                    var ucMissingItems = items.Select((value, index) => new { value, index })
                                              .Where(item => item.value == 0)
                                              .Select(item => item.index)
                                              .ToList();
                    missingItems.Add(userCollection.CollectionId, ucMissingItems);

                    var ucDuplicateItems = items.SelectMany((value, index) => Enumerable.Repeat(index, Math.Max(value - 1, 0))
                                                .Where(i => value > 1))
                                                .ToList();
                    duplicateItems.Add(userCollection.CollectionId, ucDuplicateItems);
                }

                Swapper newSwapper = new Swapper
                {
                    UserId = swapper.Id,
                    UserName = swapper.UserName,
                    MissingItems = missingItems,
                    DuplicateItems = duplicateItems
                };

                swapperList.Add(newSwapper);
            }

            var potentialSwappers = FindPotentialSwappers(currentUserId, swapperList);
            ViewBag.SelectedCollection = selectedCollection;
            ViewBag.PotentialSwappers = potentialSwappers;

            return View(model);
        }

        private List<(string, List<int>, List<int>, List<List<int>>)> FindPotentialSwappers(string currentUserId, List<Swapper> users)
        {
            var currentUser = users.FirstOrDefault(u => u.UserId == currentUserId);
            if (currentUser == null)
            {
                return new List<(string, List<int>, List<int>, List<List<int>>)>(); // User not found.
            }

            var potentialSwappers = users
                .Where(u => u.UserId != currentUserId && CanSwapWithCurrentUser(currentUser, u))
                .Select(u =>
                {
                    var (currentUserSwaps, otherUserSwaps, swaps) = GetPotentialSwaps(currentUser, u);
                    var largestTradeSize = swaps.Count > 0 ? swaps.Max(combo => combo.Count) : 0;
                    var largestTrades = swaps.Where(combo => combo.Count == largestTradeSize).ToList();
                    return (u.UserId, currentUserSwaps, otherUserSwaps, largestTrades);
                })
                .ToList();

            potentialSwappers.Sort((a, b) => b.Item4.Max(combo => combo.Count).CompareTo(a.Item4.Max(combo => combo.Count)));

            return potentialSwappers;
        }



        private (List<int>, List<int>, List<List<int>>) GetPotentialSwaps(Swapper currentUser, Swapper otherUser)
        {
            List<int> currentUserSwaps = new List<int>();
            List<int> otherUserSwaps = new List<int>();
            List<List<int>> swaps = new List<List<int>>();

            foreach (var currentKvp in currentUser.DuplicateItems)
            {
                if (otherUser.MissingItems.TryGetValue(currentKvp.Key, out var missingItems))
                {
                    var intersectingItems = currentKvp.Value.Intersect(missingItems).ToList();
                    if (intersectingItems.Any())
                    {
                        foreach (var item in intersectingItems)
                        {
                            currentUserSwaps.Add(item);
                        }
                    }
                }
            }

            foreach (var otherKvp in otherUser.DuplicateItems)
            {
                if (currentUser.MissingItems.TryGetValue(otherKvp.Key, out var missingItems))
                {
                    var intersectingItems = otherKvp.Value.Intersect(missingItems).ToList();
                    if (intersectingItems.Any())
                    {
                        foreach (var item in intersectingItems)
                        {
                            otherUserSwaps.Add(item);
                        }
                    }
                }
            }

            // Find all possible combinations of largest trade size
            int largestTradeSize = Math.Min(currentUserSwaps.Count, otherUserSwaps.Count);
            var currentUserCombos = GetCombinations(currentUserSwaps, largestTradeSize);
            var otherUserCombos = GetCombinations(otherUserSwaps, largestTradeSize);

            // Find all possible swaps between currentUserCombos and otherUserCombos
            foreach (var currentUserCombo in currentUserCombos)
            {
                foreach (var otherUserCombo in otherUserCombos)
                {
                    var swap = new List<int>();
                    swap.AddRange(currentUserCombo);
                    swap.AddRange(otherUserCombo);
                    swaps.Add(swap);
                }
            }

            return (currentUserSwaps, otherUserSwaps, swaps);
        }

        private List<List<int>> GetCombinations(List<int> items, int tradeSize)
        {
            List<List<int>> combinations = new List<List<int>>();
            GetCombinationsHelper(items, tradeSize, 0, new List<int>(), combinations);
            return combinations;
        }

        private void GetCombinationsHelper(List<int> items, int tradeSize, int index, List<int> current, List<List<int>> combinations)
        {
            if (current.Count == tradeSize)
            {
                combinations.Add(new List<int>(current));
                return;
            }

            for (int i = index; i < items.Count; i++)
            {
                current.Add(items[i]);
                GetCombinationsHelper(items, tradeSize, i + 1, current, combinations);
                current.RemoveAt(current.Count - 1);
            }
        }


        private bool CanSwapWithCurrentUser(Swapper currentUser, Swapper otherUser)
        {
            // Check if the other user's DuplicateItems contain any missing items of the current user.
            bool canSwapDuplicates = otherUser.DuplicateItems.Any(kvp =>
                currentUser.MissingItems.TryGetValue(kvp.Key, out var missingItems) &&
                kvp.Value.Intersect(missingItems).Any());

            // Check if the current user's DuplicateItems contain any missing items of the other user.
            bool canSwapMissingItems = currentUser.DuplicateItems.Any(kvp =>
                otherUser.MissingItems.TryGetValue(kvp.Key, out var missingItems) &&
                kvp.Value.Intersect(missingItems).Any());

            return canSwapDuplicates && canSwapMissingItems;
        }
    }
}