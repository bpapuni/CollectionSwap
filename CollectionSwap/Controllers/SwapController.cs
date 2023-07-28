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

            List<(string Email, List<int> yourItems, List<int> theirItems)> potentialSwappers = FindPotentialSwappers(currentUserId, swapperList);
            ViewBag.PotentialSwappers = potentialSwappers;
            ViewBag.SelectedCollection = selectedCollection;

            return View(model);
        }

        private List<(string, List<int>, List<int>)> FindPotentialSwappers(string currentUserId, List<Swapper> users)
        {
            var currentUser = users.FirstOrDefault(u => u.UserId == currentUserId);
            if (currentUser == null)
            {
                return new List<(string, List<int>, List<int>)>(); // User not found.
            }

            var potentialSwappers = users
                .Where(u => u.UserId != currentUserId && CanSwapWithCurrentUser(currentUser, u))
                .Select(u =>
                {
                    var (currentUserCombos, otherUserCombos) = GetPotentialSwaps(currentUser, u);
                    return (u.UserName, currentUserCombos, otherUserCombos);
                })
                .ToList();

            potentialSwappers.Sort((a, b) => b.Item3.Count.CompareTo(a.Item3.Count));

            return potentialSwappers;
        }



        private (List<int>, List<int>) GetPotentialSwaps(Swapper currentUser, Swapper otherUser)
        {
            List<int> currentUserSwaps = new List<int>();
            List<int> otherUserSwaps = new List<int>();
            List<List<int>> proposedSwaps = new List<List<int>>();

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

            // Find all possible combinations of largest swap size
            int largestSwapSize = Math.Min(currentUserSwaps.Count, otherUserSwaps.Count);
            var currentUserCombos = GetCombinations(currentUserSwaps, largestSwapSize);
            var otherUserCombos = GetCombinations(otherUserSwaps, largestSwapSize);

            // Find all possible swaps between currentUserCombos and otherUserCombos
            foreach (var currentUserCombo in currentUserCombos)
            {
                foreach (var otherUserCombo in otherUserCombos)
                {
                    var swap = new List<int>();
                    swap.AddRange(currentUserCombo);
                    swap.AddRange(otherUserCombo);
                    proposedSwaps.Add(swap);
                }
            }

            return (currentUserSwaps, otherUserSwaps);
            //return (currentUserCombos, otherUserCombos);
        }

        private List<List<int>> GetCombinations(List<int> items, int swapSize)
        {
            List<List<int>> combinations = new List<List<int>>();
            GetCombinationsHelper(items, swapSize, 0, new List<int>(), combinations);
            return combinations;
        }

        private void GetCombinationsHelper(List<int> items, int swapSize, int index, List<int> current, List<List<int>> combinations)
        {
            if (current.Count == swapSize)
            {
                combinations.Add(new List<int>(current));
                return;
            }

            for (int i = index; i < items.Count; i++)
            {
                current.Add(items[i]);
                GetCombinationsHelper(items, swapSize, i + 1, current, combinations);
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