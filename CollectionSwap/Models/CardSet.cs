using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace CollectionSwap.Models
{
    public class CardSet
    {
        [Key]
        public int card_set_id { get; set; }
        [Required(ErrorMessage = "Please enter a card set name.")]
        public string card_set_name { get; set; }
    }

    public class CreateCardSet
    {
        [Required(ErrorMessage = "Please enter a card set name.")]
        public string card_set_name { get; set; }

        [Required(ErrorMessage = "Please select a zip file containing card images.")]
        public HttpPostedFileBase FileInput { get; set; }
    }
}