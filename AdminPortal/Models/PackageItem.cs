using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AdminPortal.Models
{
    public class PackageItem
    {
        [Required]
        public string name { get; set; } //itemName 

        [Required]
        public string itemType { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price/Point must be greater than 0.")] //point will be sum
        public decimal priceOrPoint { get; set; } 

        [Required]
        public string ageCategory { get; set; } 

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
        public int entryQuantity { get; set; }

        [Editable(false)] // Tell MVC not to generate input form
        public int? packageItemNo { get; set; } //? mean it can be null // manual calculation
        public string? packageID { get; set; } // will be calculated manually
        public int? terminalID { get; set; } // fetch from table

    }

}
