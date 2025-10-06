using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AdminPortal.Models //declare the namespace
{
    public class PackageItem //model class to represent the package item entity
    {
        [Required]
        public string ItemName { get; set; }

        [Required]
        public string itemType { get; set; }

        [Required]
        public decimal ItemPrice { get; set; }

        [Required]
        public string AgeCategory { get; set; }

        [Required]
        public int EntryQty { get; set; }

        // This will be set from the controller after the main package is created.
        public int PackageID { get; set; }


    }

}
