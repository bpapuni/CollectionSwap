﻿using CollectionSwap.Controllers;
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
        [Display(Name = "New Collection Name")]
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
        public bool Archived { get; set; }
        public bool Charity { get; set; }
        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; }
        [ForeignKey("CollectionId")]
        public Collection Collection { get; set; }
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
        public string Delete(ApplicationDbContext db)
        {
            this.Archived = true;
            db.Entry(this).State = EntityState.Modified;
            db.SaveChanges();

            return "User Collection deleted successfully.";
        }
        public (List<Swap>, List<SwapViewModel>) FindMatchingSwaps(ApplicationDbContext db)
        {
            var userId = this.User.Id;
            // Find all user collections of the same type that don't belong to the user
            var matchingUserCollections = db.UserCollections.Where(uc => uc.CollectionId == this.CollectionId && uc.Archived == false && uc.UserId != userId).ToList();
            // Find all pending swaps the user is involved with to eliminate duplicate swap offers
            var pendingSwaps = db.Swaps
                    .Where(swap => (swap.Status == "offered" || swap.Status == "accepted" || swap.Status == "requested") && (swap.Sender.Id == userId || swap.Receiver.Id == userId))
                    .Select(swap => new { sUC = swap.SenderCollectionId, rUC = swap.ReceiverCollectionId })
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
            
            var matchingSwaps = new List<Swap>();
            var matchingSwapViews = new List<SwapViewModel>();

            foreach (var uc in matchingUserCollections)
            {
                // If the user has a pending swap with the matching user collection already, pass over this collection
                var potentialSwap = new { sUC = this.Id, rUC = uc.Id };
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
                    receiverNeededItems
                        .Select((value, index) => new { Value = value, Index = index })
                        .Where(item => item.Value != 0)
                        .Select(item => item.Index)
                        .ToList();

                    // Create the swap
                    matchingSwap.Sender = uc.User;
                    matchingSwap.Receiver = this.User;
                    matchingSwap.CollectionId = this.Collection.Id;
                    matchingSwap.Collection = this.Collection;
                    matchingSwap.SenderCollectionId = uc.Id;
                    matchingSwap.SenderCollection = uc;
                    matchingSwap.SenderRequestedItems = JsonConvert.SerializeObject(receiverNeededItems);
                    matchingSwap.ReceiverCollectionId = this.Id;
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

                    // Add both to lists to be returned as MatchingSwaps
                    matchingSwaps.Add(matchingSwap);
                    matchingSwapViews.Add(matchingSwapView);
                }
                // If sender has cards that the receiver needs AND vice-versa create the swap
                else if (senderNeededItems.Any() && receiverNeededItems.Any())
                {
                    // Create the swap
                    matchingSwap.Sender = this.User;
                    matchingSwap.Receiver = uc.User;
                    matchingSwap.CollectionId = this.Collection.Id;
                    matchingSwap.Collection = this.Collection;
                    matchingSwap.SenderCollectionId = this.Id;
                    matchingSwap.SenderCollection = this;
                    matchingSwap.SenderRequestedItems = JsonConvert.SerializeObject(receiverNeededItems);
                    matchingSwap.ReceiverCollectionId = uc.Id;
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

                    // Add both to lists to be returned as MatchingSwaps
                    matchingSwaps.Add(matchingSwap);
                    matchingSwapViews.Add(matchingSwapView);
                }
            }

            // Sort the swaps from highest swap size to lowest
            matchingSwaps = matchingSwaps
            .OrderByDescending(swap => swap.SwapSize)
            .ToList();

            return (matchingSwaps, matchingSwapViews);
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