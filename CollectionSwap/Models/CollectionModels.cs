using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace CollectionSwap.Models
{
    public class Collection
    {
        [Key]
        public int Id { get; set; }
        [Required(ErrorMessage = "Please enter a name for this collection.")]
        public string Name { get; set; }
        public string ItemListJSON { get; set; }
    }

    public class CreateCollection
    {
        [Required(ErrorMessage = "Please enter a name for this collection.")]
        public string Name { get; set; }

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
    }

    public class UserCollectionEditViewModel
    {
        public Collection collection { get; set; }
        public UserCollection userCollection { get; set; }
    }
}