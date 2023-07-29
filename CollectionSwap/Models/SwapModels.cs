using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace CollectionSwap.Models
{
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
        public string RecieverItemIdsJSON { get; set; }

        [Required]
        public string Status { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

    }
}