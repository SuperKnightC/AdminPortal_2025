using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace be_general_support_api.Models
{
    // These Models are used for Package creation, insertion, and DB mapping

    #region -- Package Frontend Insert View Model --
    // This model is used to capture package data from the front-end form
    // It includes validation logic to ensure data integrity
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
        public string? Remark2 { get; set; }
        public string? Nationality { get; set; }  // NEW

        public string? PackageNo { get; set; }

        public string? ImageID { get; set; }

        public IFormFile? PackageImage { get; set; }

        public List<PackageItem> Items { get; set; } = new List<PackageItem>();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Items == null || !Items.Any())
            {
                yield return new ValidationResult("A package must contain at least one item.", new[] { nameof(Items) });
                yield break;
            }

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
    // This model represents individual items within a package
    // It includes validation to ensure item data integrity
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
        public string? Nationality { get; set; }  // NEW
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
    // This model represents age categories for package items
    public class AgeCategory
    {
        public string AgeCode { get; set; }
        public string CategoryName { get; set; }
        public string DisplayText => $"{AgeCode} - {CategoryName}";
    }
    #endregion

    #region -- Attraction Model--
    // This model represents attractions associated with packages
    public class Attraction
    {
        public string Name { get; set; }
    }
    #endregion

    #region-- Package Image Insert Model--
    //  This model represents package images
    public class PackageImage
    {
        public int ImageID { get; set; }
        public string ImageURL { get; set; }
    }
    #endregion

    #region -- Package Model for DB Mapping --
    // This model maps to the Package table in the database
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
        public string? Remark2 { get; set; }
        public string? Nationality { get; set; }

        [NotMapped]
        public string? CreatedByName { get; set; }

        [NotMapped]
        public string? ModifiedByName { get; set; }

        [NotMapped]
        public string? CreatedByFirstName { get; set; }

        [NotMapped]
        public string? ModifiedByFirstName { get; set; }
    }
    #endregion

    #region -- Status Model --
    // This model is used for updating package status
    public class StatusUpdateModel
    {
        public string Status { get; set; }
        public string? FinanceRemark { get; set; }
    }
    #endregion
}