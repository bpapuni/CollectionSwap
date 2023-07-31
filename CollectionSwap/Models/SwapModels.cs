using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace CollectionSwap.Models
{
    public class FindSwapsViewModel
    {
        public List<ApplicationUser> Users { get; set; }
        public List<Collection> Collections { get; set; }
        public List<UserCollection> UserCollections { get; set; }
        public List<Swap> OfferedSwaps { get; set; }
        public List<Swap> AcceptedSwaps { get; set; }
    }

    public class SwapViewModel
    {
        public string UserName { get; set; }
        public List<string> ItemList { get; set; }
        public string ImagePath { get; set; }
        public List<int> SenderItemIds { get; set; }
        public List<int> ReceiverItemIds { get; set; }
        public int SwapSize { get; set; }
        public string Type { get; set; }
}

    public class Swap
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public int CollectionId { get; set; }
        [Required]
        public int SenderUserCollectionId { get; set; }
        [Required]
        public int ReceiverUserCollectionId { get; set; }
        [Required]
        public string SenderId { get; set; }
        [Required]
        public string ReceiverId { get; set; }
        [Required]
        public string SenderItemIdsJSON { get; set; }
        [Required]
        public string ReceiverItemIdsJSON { get; set; }
        [Required]
        public string Status { get; set; }
        [Required]
        public DateTimeOffset StartDate { get; set; }
        public DateTimeOffset? EndDate { get; set; }
        [ForeignKey("SenderId")]
        public ApplicationUser Sender { get; set; }
        [ForeignKey("ReceiverId")]
        public ApplicationUser Receiver { get; set; }
    }
}