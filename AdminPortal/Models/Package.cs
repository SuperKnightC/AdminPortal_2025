using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AdminPortal.Models
{
    //This Model is for package creation and insertion to the database

    #region -- Package Frontend Insert View Model --
    public class PackageViewModel : IValidatableObject
    {
        [Required(ErrorMessage = "Package Name is required.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Package Type is required.")]
        public string packageType { get; set; }

        [Required]
        public decimal Price { get; set; }

        [Required]
        public int Point { get; set; }

        [Required(ErrorMessage = "Effective Date is required.")]
        public DateTime effectiveDate { get; set; }

        [Required(ErrorMessage = "Last Valid Date is required.")]
        public DateTime LastValidDate { get; set; }

        public string? remark { get; set; }

        public string? PackageNo { get; set; }

        public string? ImageID { get; set; }

        public IFormFile? PackageImage { get; set; }

        public List<PackageItem> Items { get; set; } = new List<PackageItem>();

        // This is your custom validation logic from before
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (packageType == "Entry" && Price <= 0)
            {
                yield return new ValidationResult("Price is required for Entry type packages.", new[] { nameof(Price) });
            }
            if ((packageType == "Point" || packageType == "Reward") && Point <= 0)
            {
                yield return new ValidationResult("Point value is required for Point or Reward type packages.", new[] { nameof(Point) });
            }
        }
    }
    #endregion

    #region-- PackageItem Model for DB Mapping --
    public class PackageItem : IValidatableObject
    {
        [Required]
        public string ItemName { get; set; }

        [Required]
        public string itemType { get; set; }

        [NotMapped]
        public decimal Value { get; set; }

        public decimal? Price { get; set; }

        public int? Point { get; set; }

        [Required]
        public string AgeCategory { get; set; }

        [Required]
        public int EntryQty { get; set; }

        public int PackageID { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (itemType == "Entry" || itemType == "Point" || itemType == "Reward")
            {
                if (Value <= 0)
                {
                    yield return new ValidationResult("A value greater than zero is required.", new[] { nameof(Value) });
                }
            }
        }
    }
    #endregion

    #region-- AgeCategory Model--

    // Pull Age Category from db
    public class AgeCategory
    {
        public string AgeCode { get; set; }
        public string CategoryName { get; set; }
        public string DisplayText => $"{AgeCode} - {CategoryName}";
    }
    #endregion

    #region -- Attraction Model--
    // Get Attraction from db
    public class Attraction
    {
        public string Name { get; set; }
    }
    #endregion

    #region-- Package Image Insert Model--
    // PackageImages Insert to db
    public class PackageImage
    {
        public int ImageID { get; set; }
        public string ImageURL { get; set; }
    }
    #endregion

    #region -- Package Model for DB Mapping --
    public class Package
    {
        public int PackageID { get; set; }
        public string? PackageNo { get; set; }
        public string Name { get; set; }
        public string PackageType { get; set; }
        public decimal Price { get; set; }
        public int Point { get; set; }
        public int ValidDays { get; set; }
        public int DaysPass { get; set; }
        public DateTime LastValidDate { get; set; }
        public string? Link { get; set; }
        public string Status { get; set; }
        public DateTime CreatedDate { get; set; }
        public int CreatedUserID { get; set; }
        public DateTime ModifiedDate { get; set; }
        public int ModifiedUserID { get; set; }
        public int GroupEntityID { get; set; }
        public int TerminalGroupID { get; set; }
        public long ProductID { get; set; }
        public string? ImageID { get; set; }
        public string? Remark { get; set; }

    }
    #endregion
}