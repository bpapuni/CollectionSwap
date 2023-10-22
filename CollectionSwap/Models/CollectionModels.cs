using CollectionSwap.Controllers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Hosting;
using System.Collections.ObjectModel;
using System.Data.Entity.Core.Metadata.Edm;
using System.Drawing.Imaging;
using System.Drawing;
using Newtonsoft.Json.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Runtime.Remoting.Lifetime;
using System.Web.Mvc;

namespace CollectionSwap.Models
{
    public class Collection
    {
        [Key]
        public int Id { get; set; }
        [Required(ErrorMessage = "Please enter a name for this collection.")]
        public string Name { get; set; }
        public string Description { get; set; }
        public string ItemListJSON { get; set; }
        public virtual Sponsor Sponsor { get; set; }
        public static bool Create(CreateCollectionModel collection, ApplicationDbContext db)
        {
            if (collection.fileInput != null && collection.fileInput.ContentLength > 0)
            {
                // Access the contents of the zip file
                using (ZipArchive archive = new ZipArchive(collection.fileInput.InputStream, ZipArchiveMode.Read))
                {
                    var entryList = archive.Entries.ToList();
                    entryList = entryList.OrderBy(entry => entry.Name.Length).ToList();


                    var newCollection = new Collection 
                    { 
                        Name = collection.Name,
                        Description = collection.Description,
                    };
                    string extractPath = null;
                    List<string> fileNames = new List<string>();
                    int i = 1;
                    // Loop through the entries in the archive and add file names to the list
                    foreach (ZipArchiveEntry entry in entryList)
                    {
                        if (!string.IsNullOrEmpty(entry.Name))
                        {
                            string fileExtension = Path.GetExtension(entry.Name).ToLower();

                            // Check if the file extension is jpg or png
                            if (fileExtension == ".jpg" || fileExtension == ".png")
                            {
                                // Ensure the target directory exists; create it if it doesn't
                                if (extractPath == null)
                                {
                                    // Generate the extractPath only once when a valid file is found
                                    db.Collections.Add(newCollection);
                                    db.SaveChanges(); // SaveChanges here to generate the newCollection.Id
                                    extractPath = HostingEnvironment.MapPath("~/Collections/" + newCollection.Id);

                                    if (!Directory.Exists(extractPath))
                                    {
                                        Directory.CreateDirectory(extractPath);
                                    }
                                }

                                string fileName = i.ToString() + fileExtension;
                                string extractedFilePath = Path.Combine(extractPath, fileName);
                                entry.ExtractToFile(extractedFilePath, true);

                                fileNames.Add(fileName);
                                i++;
                            }
                        }
                    }

                    if (fileNames.Count() > 0)
                    {
                        var orderedFiles = fileNames.OrderBy(f => f.Length);
                        newCollection.ItemListJSON = JsonConvert.SerializeObject(orderedFiles);
                        db.Entry(newCollection).State = EntityState.Modified;
                        db.SaveChanges();
                        return true;
                    }
                }
            }
            return false;
        }
        public void Update(string Name, ApplicationDbContext db)
        {
            this.Name = Name;
            db.Entry(this).State = EntityState.Modified;
            db.SaveChanges();
        }
        public void Delete(ApplicationDbContext db)
        {
            db.Collections.Remove(this);
            db.SaveChanges();

            string directoryPath = HostingEnvironment.MapPath("~/Collections/" + this.Id);
            if (Directory.Exists(directoryPath))
            {
                // Delete the directory and its content (recursive: true)
                Directory.Delete(directoryPath, recursive: true);
            }
        }
        public void AddItem(HttpPostedFileBase fileInput, ApplicationDbContext db)
        {
            if (fileInput != null && fileInput.ContentLength > 0)
            {
                string path = HostingEnvironment.MapPath("~/Collections/" + this.Id);
                string[] files = Directory.GetFiles(path);
                files = files.Select(file => Path.GetFileNameWithoutExtension(file)).ToArray();
                var sortedFiles = files.OrderBy(f => f.Length);
                int newFileName = Int32.Parse(sortedFiles.Last()) + 1;
                string filePath = path + '/' + newFileName.ToString() + ".png";

                Image img = Image.FromStream(fileInput.InputStream);
                img.Save(filePath, ImageFormat.Png);

                var updatedItemList = JsonConvert.DeserializeObject<List<string>>(this.ItemListJSON);
                updatedItemList.Add(newFileName.ToString() + ".png");
                this.ItemListJSON = JsonConvert.SerializeObject(updatedItemList);

                var affectedUserCollections = db.UserCollections.Where(uc => uc.CollectionId == this.Id).ToList();
                foreach (var userCollection in affectedUserCollections)
                {
                    var itemList = JsonConvert.DeserializeObject<List<int>>(userCollection.ItemCountJSON);
                    itemList.Add(0);
                    userCollection.ItemCountJSON = JsonConvert.SerializeObject(itemList);
                    db.Entry(userCollection).State = EntityState.Modified;
                }

                db.Entry(this).State = EntityState.Modified;
                db.SaveChanges();
            }
        }
        public void EditItem(int itemId, string fileName, HttpPostedFileBase fileInput, ApplicationDbContext db)
        {
            if (fileInput != null && fileInput.ContentLength > 0)
            {
                fileName = fileName.Split('?')[0];
                string filePath = HostingEnvironment.MapPath("~/Collections/" + this.Id + '/' + fileName);
                fileInput.SaveAs(filePath);

                string cacheBuster = DateTime.UtcNow.Ticks.ToString();
                List<string> updatedItemList = JsonConvert.DeserializeObject<List<string>>(this.ItemListJSON);
                updatedItemList[itemId] = fileName + $"?time={cacheBuster}";
                this.ItemListJSON = JsonConvert.SerializeObject(updatedItemList);

                db.Entry(this).State = EntityState.Modified;
                db.SaveChanges();
            }
        }
        public void DeleteItem(string fileName, ApplicationDbContext db)
        {
            string filePath = HostingEnvironment.MapPath("~/Collections/" + this.Id + '/' + fileName.Split('?')[0]);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);

                var updatedItemList = JsonConvert.DeserializeObject<List<string>>(this.ItemListJSON);
                var itemIndex = updatedItemList.IndexOf(fileName);
                updatedItemList.Remove(fileName);
                this.ItemListJSON = JsonConvert.SerializeObject(updatedItemList);

                var affectedUserCollections = db.UserCollections.Where(uc => uc.CollectionId == this.Id).ToList();
                foreach (var userCollection in affectedUserCollections)
                {
                    var itemList = JsonConvert.DeserializeObject<List<int>>(userCollection.ItemCountJSON);
                    itemList.RemoveAt(itemIndex);
                    userCollection.ItemCountJSON = JsonConvert.SerializeObject(itemList);
                    db.Entry(userCollection).State = EntityState.Modified;
                }

                db.Entry(this).State = EntityState.Modified;
                db.SaveChanges();
            }
        }
    }

    public class EditCollectionModel
    {
        public Collection Collection { get; set; }
        [Required(ErrorMessage = "Please select an image.")]
        [Display(Name = "Add an Item")]
        public HttpPostedFileBase FileInput { get; set; }
    }

    public class CreateCollectionModel
    {
        [Display(Name = "New Collection Name")]
        [Required(ErrorMessage = "Please enter a name for this collection.")]
        public string Name { get; set; }
        [StringLength(60, ErrorMessage = "Description must be shorter than 60 characters.")]
        [Display(Name = "Description (optional)")]
        public string Description { get; set; }

        [ZipFile(ErrorMessage = "Please select a zip file containing images.")]
        [Required(ErrorMessage = "Please select a zip file containing images.")]
        public HttpPostedFileBase fileInput { get; set; }
    }

    public class UserCollection
    {
        [Key]
        public int Id { get; set; }
        [Required(ErrorMessage = "Please enter a name for this collection.")]
        public string Name { get; set; }
        [Required]
        public string UserId { get; set; }
        [Required]
        public int CollectionId { get; set; }
        public string ItemCountJSON { get; set; }
        public bool Archived { get; set; }
        public bool Charity { get; set; }
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }
        [ForeignKey("CollectionId")]
        public virtual Collection Collection { get; set; }
        public static UserCollection Create(int id, string userId, ApplicationDbContext db)
        {
            var collection = db.Collections.Find(id);
            var newUserCollection = new UserCollection()
            {
                Name = collection.Name,
                UserId = userId,
                CollectionId = id,
                ItemCountJSON = JsonConvert.SerializeObject(new List<int>(Enumerable.Repeat(0, JsonConvert.DeserializeObject<List<string>>(collection.ItemListJSON).Count)))
            };

            db.UserCollections.Add(newUserCollection);
            db.SaveChanges();

            return newUserCollection;
        }
        public void Update(string property, string userId, string value, ApplicationDbContext db)
        {
            // Return early if update method was somehow called by anyone besides the user
            if (this.UserId != userId)
            {
                return;
            }

            switch (property)
            {
                case "Name":
                    this.Name = value;
                    break;
                case "Quantity":
                    var deserializedItemCount = JsonConvert.DeserializeObject<List<int>>(this.ItemCountJSON);
                    var jsonValue = JsonConvert.DeserializeObject<dynamic>(value);
                    int index = jsonValue.index;
                    deserializedItemCount[index] = jsonValue.quantity;
                    this.ItemCountJSON = JsonConvert.SerializeObject(deserializedItemCount);
                    break;
                case "Charity":
                    this.Charity = this.Charity ? false : true;
                    break;
                default:
                    break;
            }
            db.Entry(this).State = EntityState.Modified;
            db.SaveChanges();
        }
        public void Delete(ApplicationDbContext db)
        {
            var affectedSwaps = db.Swaps.Where(swap => (swap.SenderCollection.Id == this.Id || swap.ReceiverCollection.Id == this.Id)).ToList();
            var requestedSwaps = affectedSwaps.Where(swap => swap.Status == "requested");
            var acceptedSwaps = affectedSwaps.Where(swap => swap.Status == "accepted");
            var confirmedSwaps = affectedSwaps.Where(swap => swap.Status == "confirmed");

            foreach(var swap in affectedSwaps)
            {
                if (requestedSwaps.Contains(swap) || acceptedSwaps.Contains(swap))
                {
                    swap.ProcessSwap(this.UserId, new SwapRequestViewModel { Status = "canceled" }, db);
                }

                // Prevent deleting until confirmed swaps are done
            }

            if (affectedSwaps.Count == 0)
            {
                db.UserCollections.Remove(this);
            }
            else
            {
                this.Archived = true;
                db.Entry(this).State = EntityState.Modified;
            }

            db.SaveChanges();
        }
        public List<SwapViewModel> FindMatchingSwaps(ApplicationDbContext db)
        {
            var userId = this.User.Id;
            var blockedUsers = this.User.BlockedUsers != null ? this.User.BlockedUsers : "";
            var usersBlockingYou = string.Join(",", db.Users.Where(u => u.BlockedUsers.Contains(userId)).Select(u => u.Id).ToList());
            // Find all user collections of the same type that don't belong to the user, aren't blocked by you, and aren't blocking you
            var matchingUserCollections = db.UserCollections
                .Where(uc => uc.CollectionId == this.CollectionId && 
                uc.Archived == false && 
                uc.UserId != userId && 
                uc.User.ClosedAccount == false &&
                !blockedUsers.Contains(uc.UserId) && 
                !usersBlockingYou.Contains(uc.UserId)).
                ToList();
            // Find all pending swaps the user is involved with to eliminate duplicate swap offers
            var pendingSwaps = db.Swaps.Where(swap => (swap.Status == "offered" || swap.Status == "accepted" || swap.Status == "requested") && 
                (swap.Sender.Id == userId || swap.Receiver.Id == userId))
                .Select(swap => new { sUC = swap.SenderCollection.Id, rUC = swap.ReceiverCollection.Id })
                .ToList();
            // Get users current items
            var senderItems = JsonConvert.DeserializeObject<List<int>>(this.ItemCountJSON);
            // Get users missing items
            var missingItems = senderItems
                .Select((value, index) => new { Value = value, Index = index })
                .Where(item => item.Value == 0)
                .Select(item => item.Index)
                .ToList();
            // Get users duplicate items
            var duplicateItems = senderItems
                .SelectMany((value, index) => Enumerable.Repeat(index, Math.Max(value - 1, 0))
                .Where(i => value > 1))
                .ToList();

            var matchingSwapViews = new List<SwapViewModel>();

            foreach (var uc in matchingUserCollections)
            {
                // If the user has a pending swap with the matching user collection already, pass over this collection
                // If the uc is flagged as charity the sender and receiver user collections are swapped
                var potentialSwap = uc.Charity ? new { sUC = uc.Id, rUC = this.Id } : new { sUC = this.Id, rUC = uc.Id };
                if (pendingSwaps.Contains(potentialSwap))
                {
                    continue;
                }

                // Get items in matching user collection
                var ucItems = JsonConvert.DeserializeObject<List<int>>(uc.ItemCountJSON);
                // Get the items missing from the matching user collection
                var ucMissingItems = ucItems
                    .Select((value, index) => new { Value = value, Index = index })
                    .Where(item => item.Value == 0)
                    .Select(item => item.Index)
                    .ToList();
                // Get the duplicate items in the matching user collection
                var ucDuplicateItems = ucItems
                .SelectMany((value, index) => Enumerable.Repeat(index, Math.Max(value - 1, 0))
                .Where(i => value > 1))
                .ToList();
                // Items in both the users missing items AND matching user collection duplicate items are the senders needed items
                var senderNeededItems = missingItems.Intersect(ucDuplicateItems).ToList();
                // Items in both the users duplicate items AND matching user collection's missing items are the receivers needed items
                var receiverNeededItems = duplicateItems.Intersect(ucMissingItems).ToList();

                var matchingSwap = new Swap();
                // If the matching user collection is flagged for charity switch sender and receivers roles
                if (uc.Charity)
                {
                    // sender is now the other user and needs none of the receiver's (user's) items
                    senderNeededItems = new List<int>();
                    // All items in the senders user collection are needed by the receiver
                    receiverNeededItems = ucItems
                        .Select((value, index) => new { Value = value, Index = index })
                        .Where(item => item.Value != 0)
                        .Select(item => item.Index)
                        .ToList();

                    // Create the swap
                    matchingSwap.Sender = uc.User;
                    matchingSwap.Receiver = this.User;
                    matchingSwap.Collection = this.Collection;
                    matchingSwap.SenderCollection = uc;
                    matchingSwap.SenderRequestedItems = JsonConvert.SerializeObject(receiverNeededItems);
                    matchingSwap.ReceiverCollection = this;
                    matchingSwap.ReceiverRequestedItems = JsonConvert.SerializeObject(new List<int>());
                    matchingSwap.SwapSize = 0;
                    matchingSwap.Status = "charity";

                    // Create corresponding swap view to display the swap with its validation
                    var matchingSwapView = new SwapViewModel
                    {
                        Swap = matchingSwap,
                        Validation = matchingSwap.Validate(userId, db)
                    };

                    // Add the view to the list to be returned as MatchingSwaps
                    matchingSwapViews.Add(matchingSwapView);
                }
                // If sender has cards that the receiver needs AND vice-versa create the swap
                else if (senderNeededItems.Any() && receiverNeededItems.Any())
                {
                    // Create the swap
                    matchingSwap.Sender = this.User;
                    matchingSwap.Receiver = uc.User;
                    matchingSwap.Collection = this.Collection;
                    matchingSwap.SenderCollection = this;
                    matchingSwap.SenderRequestedItems = JsonConvert.SerializeObject(receiverNeededItems);
                    matchingSwap.ReceiverCollection = uc;
                    matchingSwap.ReceiverRequestedItems = JsonConvert.SerializeObject(senderNeededItems);
                    matchingSwap.SwapSize = Math.Min(senderNeededItems.Count(), receiverNeededItems.Count());
                    matchingSwap.Status = "swap";

                    // Create corresponding swap view to display the swap with its validation
                    var matchingSwapView = new SwapViewModel
                    {
                        Swap = matchingSwap,
                        Validation = matchingSwap.Validate(userId, db)
                    };

                    // Add the view to the list to be returned as MatchingSwaps
                    matchingSwapViews.Add(matchingSwapView);
                }
            }

            // Sort the swaps from highest swap size to lowest
            matchingSwapViews = matchingSwapViews
                .OrderBy(view => view.Swap.SwapSize == 0 ? 0 : 1)   // If SwapSize is 0, swap is charitable, so display it first
                .ThenByDescending(view => view.Swap.SwapSize)       // Then order by SwapSize
                .ToList();

            return matchingSwapViews;
        }
    }

    public class CollectionButton
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public List<string> ItemList { get; set; }
        public int SetSize { get; set; }
        public int Duplicates { get; set; }
        public string Type { get; set; }
    }

    public class Sponsor
    {
        public int Id { get; set; }
        public int CollectionId { get; set; }
        public string Statement { get; set; }
        public string Url { get; set; }
        public string Image { get; set; }
        public void Edit(int collectionId, HttpPostedFileBase fileInput, string statement, string url, ApplicationDbContext db)
        {
            if (fileInput != null && fileInput.ContentLength > 0)
            {
                string fileExtension = Path.GetExtension(fileInput.FileName).ToLower();

                // Check if the file extension is jpg or png
                if (fileExtension == ".jpg" || fileExtension == ".png")
                {
                    string fileName = this.Id != 0 ? this.Image.Split('?')[0] : Guid.NewGuid().ToString() + fileExtension;
                    string cacheBuster = DateTime.UtcNow.Ticks.ToString();
                    
                    this.CollectionId = collectionId;
                    this.Image = fileName + $"?time={cacheBuster}";

                    if (this.Id == 0)
                    {
                        // Generate the new sponsor id
                        db.Sponsors.Add(this);
                        db.SaveChanges();
                    }
                    else
                    {
                        db.Entry(this).State = EntityState.Modified;
                    }

                    var collection = db.Collections.Find(collectionId);
                    if (collection != null) { 
                        collection.Sponsor = this;
                        db.Entry(collection).State = EntityState.Modified;
                    }

                    db.SaveChanges();

                    var extractPath = HostingEnvironment.MapPath("~/Sponsors/" + collectionId);
                    if (!Directory.Exists(extractPath))
                    {
                        Directory.CreateDirectory(extractPath);
                    }

                    fileInput.SaveAs(extractPath + '/' + fileName);
                }                
            }
            // If no file is uploaded, admin has only editted the url
            else if (this.Id != 0)
            {
                this.Url = url;
                db.Entry(this).State = EntityState.Modified;
                db.SaveChanges();
            }
            if (statement != null)
            {
                this.Statement = HttpUtility.HtmlEncode(statement);
            }
            if (url != null)
            {
                this.Url = HttpUtility.HtmlEncode(url);
            }
            db.Entry(this).State = EntityState.Modified;
            db.SaveChanges();
        }
        public void Delete(ApplicationDbContext db)
        {
            string filePath = HostingEnvironment.MapPath($"~/Sponsors/{this.CollectionId}/" + this.Image.Split('?')[0]);
            if (File.Exists(filePath))
            {
                if (this.CollectionId != 0)
                {
                    var collection = db.Collections.Where(c => c.Sponsor.Id == this.Id).FirstOrDefault();
                    collection.Sponsor = null;
                    db.Entry(collection).State = EntityState.Modified;
                }

                File.Delete(filePath);
                db.Sponsors.Remove(this);
                db.SaveChanges();
            }
        }
    }

    public class CreateSponsorModel
    {
        public int Id { get; set; }
        public string Url { get; set; }
        public string Image { get; set; }
        [Required(ErrorMessage = "Please select a .jpg or .png file.")]
        public HttpPostedFileBase FileInput { get; set; }
    }

    public class HeldItems
    {
        [Key]
        public int Id { get; set; }
        public string ItemListJSON { get; set; }
        public virtual UserCollection UserCollection { get; set; }
        public virtual Swap Swap { get; set; }

    }
}