// AdminPortal.Models/Package.cs

using AdminPortal.Models;
using System.ComponentModel.DataAnnotations;


namespace AdminPortal.Models
{
    public class PackageViewModel
    {
        [Required(ErrorMessage = "Package Name is required.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Package Type is required.")]
        public string packageType { get; set; }

        [Required]
        public decimal Price { get; set; } // <-- ADD THIS

        [Required]
        public int Point { get; set; } // <-- ADD THIS

        [Required(ErrorMessage = "Effective Date is required.")]
        public DateTime effectiveDate { get; set; }

        [Required(ErrorMessage = "Last Valid Date is required.")]
        public DateTime LastValidDate { get; set; }

        public string? remark { get; set; }

        // Make these nullable so they don't cause validation issues
        public string? PackageNo { get; set; }
        public string? ImageID { get; set; }

        public List<PackageItem> Items { get; set; } = new List<PackageItem>();
    }
}