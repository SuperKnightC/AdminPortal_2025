using System;
using System.Collections.Generic;

namespace be_general_support_api.Models
{
    //This Models are essentially used for displaying Packages and Its details on the dashboard pages

    #region-- Package Summary View Model -- 
    //This is the big view, front dashboard view
    public class PackageSummaryViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string PackageType { get; set; }
        public int EntryQty { get; set; }
        public string Category { get; set; }
        public string? Nationality { get; set; } // ✅ ADDED
        public string ImageUrl { get; set; }
        public string Status { get; set; }
        public decimal Price { get; set; }
        public int Point { get; set; }
        public DateTime DateCreated { get; set; }
    }
    #endregion

    #region-- Package Detail View Model --
    // Detailed view after clicking on a package from the summary view
    public class PackageDetailViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string PackageType { get; set; }
        public int TotalEntryQty { get; set; }
        public decimal Price { get; set; }
        public int Point { get; set; }
        public string AgeCategory { get; set; }
        public string Nationality { get; set; }
        public DateTime EffectiveDate { get; set; }
        public DateTime LastValidDate { get; set; }
        public int ValidDays { get; set; }
        public string Status { get; set; }
        public string ImageUrl { get; set; }
        public string Remark { get; set; }
        public DateTime CreatedDate { get; set; }
        public string SubmittedBy { get; set; }  // CreatedByFirstName
        public string ApprovedBy { get; set; }   // ModifiedByFirstName

        public List<PackageItemDetail> Items { get; set; } = new List<PackageItemDetail>();
    }
    #endregion

    #region-- Helper Class for Package Item Details --
    // Display each attraction/item within a package
    public class PackageItemDetail
    {
        public string ItemName { get; set; }
        public decimal Price { get; set; }
        public int Point { get; set; }
        public int EntryQty { get; set; }
        public string Nationality { get; set; }
        public string Category { get; set; }
    }
    #endregion
}