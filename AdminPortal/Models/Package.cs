using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AdminPortal.Models
{
  
    public class PackageViewModel //view model to handle the data from view to controller
    {
        [Required(ErrorMessage = "Package Name is required.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Package Type is required.")]
        public string packageType { get; set; }

        [Required(ErrorMessage = "Effective Date is required.")]
        public DateTime effectiveDate { get; set; }

        [Required(ErrorMessage = "Last Valid Date is required.")]
        public DateTime LastValidDate { get; set; }

        public string? remark { get; set; }
        public int validDays { get; set; }
        public List<PackageItem> Items { get; set; } = new List<PackageItem>();

    }
}
