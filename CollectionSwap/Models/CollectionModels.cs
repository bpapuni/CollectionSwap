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

namespace CollectionSwap.Models
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

    public class Collection
    {
        [Key]
        public int Id { get; set; }
        [Required(ErrorMessage = "Please enter a name for this collection.")]
        public string Name { get; set; }
        public string ItemListJSON { get; set; }
        public static void Create(CreateCollectionModel collection, ApplicationDbContext db)
        {
            if (collection.fileInput != null && collection.fileInput.ContentLength > 0)
            {
                // Access the contents of the zip file
                using (ZipArchive archive = new ZipArchive(collection.fileInput.InputStream, ZipArchiveMode.Read))
                {
                    var entryList = archive.Entries.ToList();
                    entryList = entryList.OrderBy(entry => entry.Name.Length).ToList();

                    // Save the collection to the database so it is allocated an Id
                    Collection newCollection = new Collection()
                    {
                        Name = collection.Name
                    };
                    db.Collections.Add(newCollection);
                    db.SaveChanges();

                    // Specify the path where to save the uploaded file on the server
                    string extractPath = HostingEnvironment.MapPath("~/Collections/" + newCollection.Id);

                    // Ensure the target directory exists; create it if it doesn't
                    if (!Directory.Exists(extractPath))
                    {
                        Directory.CreateDirectory(extractPath);
                    }

                    List<string> fileNames = new List<string>();
                    int i = 1;
                    // Loop through the entries in the archive and add file names to the list
                    foreach (ZipArchiveEntry entry in entryList)
                    {
                        if (!string.IsNullOrEmpty(entry.Name))
                        {
                            string fileName = i.ToString() + Path.GetExtension(entry.Name);
                            string extractedFilePath = Path.Combine(extractPath, fileName);
                            entry.ExtractToFile(extractedFilePath, true);

                            fileNames.Add(fileName);
                            i++;
                        }
                    }

                    var orderedFiles = fileNames.OrderBy(f => f.Length);
                    newCollection.ItemListJSON = JsonConvert.SerializeObject(orderedFiles);
                    db.Entry(newCollection).State = EntityState.Modified;
                    db.SaveChanges();
                }
            }
        }
        public void Refresh(ApplicationDbContext db)
        {
            string path = HostingEnvironment.MapPath("~/Collections/" + this.Id);
            string[] files = Directory.GetFiles(path);
            files = files.Select(fileName => Path.GetFileName(fileName)).ToArray();
            var orderedFiles = files.OrderBy(f => f.Length);
            this.ItemListJSON = JsonConvert.SerializeObject(orderedFiles);
            db.SaveChanges();
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

                List<string> updatedItemList = JsonConvert.DeserializeObject<List<string>>(this.ItemListJSON);
                updatedItemList.Add(newFileName.ToString() + ".png");
                this.ItemListJSON = JsonConvert.SerializeObject(updatedItemList);

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

                List<string> updatedItemList = JsonConvert.DeserializeObject<List<string>>(this.ItemListJSON);
                updatedItemList.Remove(fileName);
                this.ItemListJSON = JsonConvert.SerializeObject(updatedItemList);

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
        [Display(Name = "Collection Name")]
        [Required(ErrorMessage = "Please enter a name for this collection.")]
        public string Name { get; set; }
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
        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; }
        public static UserCollection Create(int id, int[] quantity, ApplicationDbContext db, string userId)
        {
            UserCollection newUserCollection = new UserCollection()
            {
                Name = db.Collections.Find(id).Name,
                UserId = userId,
                CollectionId = id,
                ItemCountJSON = JsonConvert.SerializeObject(quantity)
            };

            db.UserCollections.Add(newUserCollection);
            db.SaveChanges();

            return newUserCollection;
        }
        public void Update(string name, ApplicationDbContext db)
        {
            this.Name = name;
            db.Entry(this).State = EntityState.Modified;
            db.SaveChanges();
        }
        public string Delete(ApplicationDbContext db)
        {
            UserCollection userCollection = db.UserCollections.Find(this.Id);
            db.UserCollections.Remove(userCollection);
            db.SaveChanges();

            return "User Collection deleted successfully.";
        }
        public List<MatchingSwap> FindMatchingSwaps(ApplicationDbContext db)
        {
            var currentUserId = this.User.Id;
            var potentialSwapList = FindPotentialSwaps(this, db);

            PotentialSwap currentUserSwapper = potentialSwapList.Where(swap => swap.User.Id == currentUserId && swap.UserCollection == this).FirstOrDefault();

            List<MatchingSwap> matchingSwaps = new List<MatchingSwap>();

            if (currentUserSwapper == null)
            {
                return new List<MatchingSwap>(); // User not found.
            }


            foreach (var potentialSwap in potentialSwapList)
            {
                // Find all pending swaps for the current user
                var pendingSwaps = db.Swaps
                    .Where(swap => (swap.Status == "offered" || swap.Status == "accepted") && (swap.Sender.Id == currentUserId || swap.Receiver.Id == currentUserId))
                    .Select(swap => new { swap.SenderUserCollectionId, swap.ReceiverUserCollectionId })
                    .ToList();

                // Skip over this potentialSwap if it belongs to the current user or 
                // if the current user already has a pending swap matching this potentialSwap
                if (potentialSwap == currentUserSwapper) { continue; }
                else if (pendingSwaps.Any(swap => (swap.SenderUserCollectionId == this.Id && swap.ReceiverUserCollectionId == potentialSwap.UserCollection.Id) || (swap.SenderUserCollectionId == potentialSwap.UserCollection.Id && swap.ReceiverUserCollectionId == this.Id))) { continue; }

                var currentUserNeededItems = currentUserSwapper.MissingItems.Intersect(potentialSwap.DuplicateItems).ToList();
                var otherUserNeededItems = currentUserSwapper.DuplicateItems.Intersect(potentialSwap.MissingItems).ToList();

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
        private List<PotentialSwap> FindPotentialSwaps(UserCollection selectedCollection, ApplicationDbContext db)
        {
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

            return swapList;
        }
    }

    public class CollectionButton
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string href { get; set; }
        public List<string> ItemList { get; set; }
    }

    public class UserCollectionModel
    {
        public Collection Collection { get; set; }
        public UserCollection UserCollection { get; set; }
    }

    public class HeldItems
    {
        [Key]
        public int Id { get; set; }
        public string ItemListJSON { get; set; }
        public UserCollection UserCollection { get; set; }
        public Swap Swap { get; set; }

    }
}