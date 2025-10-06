using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AdminPortal.Models
{
  
    public class PackageViewModel
    {
        [Required(ErrorMessage = "Package Name is required.")]
        public string name { get; set; } 

        [Required(ErrorMessage = "Package Type is required.")]
        public string packageType { get; set; } 
        public string imageID { get; set; } 

        [Required(ErrorMessage = "Last Valid Day is required.")]
        [DataType(DataType.Date)]
        public DateTime LastValidDate { get; set; } 

        [Required(ErrorMessage = "Effective Date is required.")]
        [DataType(DataType.Date)]
        public DateTime effectiveDate { get; set; } 

        public string remark { get; set; }

       
        [Editable(false)] // Tell MVC not to generate input form
        public int? validDay { get; set; } //? mean it can be null
        public int? terminalID { get; set; } // fetch from table


        // This list holds all the dynamically added items (the "Create more Package Item?" loop)
        public List<PackageItem> Items { get; set; } = new List<PackageItem>();

    }
}
