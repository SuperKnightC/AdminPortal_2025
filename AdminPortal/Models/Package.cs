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

        // --- THIS IS THE FIX ---
        // This custom validation logic now calculates the total price/points
        // from the items list before checking for validity.
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // First, check if there are any items at all.
            if (Items == null || !Items.Any())
            {
                yield return new ValidationResult("A package must contain at least one item.", new[] { nameof(Items) });
                // Stop further validation if there are no items.
                yield break;
            }

            // Calculate the totals based on the items provided.
            decimal calculatedPrice = 0;
            int calculatedPoints = 0;
            foreach (var item in Items)
            {
                if (item.itemType == "Entry")
                {
                    calculatedPrice += item.Value * item.EntryQty;
                }
                else if (item.itemType == "Point" || item.itemType == "Reward")
                {
                    calculatedPoints += (int)item.Value * item.EntryQty;
                }
            }

            // Now, validate against the calculated totals.
            if (packageType == "Entry" && calculatedPrice <= 0)
            {
                yield return new ValidationResult("Price is required for Entry type packages. The total price of items must be greater than 0.", new[] { nameof(Price) });
            }
            if ((packageType == "Point" || packageType == "Reward") && calculatedPoints <= 0)
            {
                yield return new ValidationResult("Point value is required for Point or Reward type packages. The total points of items must be greater than 0.", new[] { nameof(Point) });
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

        [NotMapped] // This attribute tells EF Core not to create a column for this
        public string? CreatedByName { get; set; }

        [NotMapped]
        public string? ModifiedByName { get; set; }

    }
    #endregion

    #region -- Status Model --
    public class StatusUpdateModel
    {
        public string Status { get; set; }
    }
    #endregion
}