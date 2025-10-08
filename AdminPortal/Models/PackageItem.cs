using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // Required for [NotMapped]

namespace AdminPortal.Models
{
    public class PackageItem : IValidatableObject
    {
        [Required]
        public string ItemName { get; set; }

        [Required]
        public string itemType { get; set; }

        // This property is only for capturing the form input, it won't be saved to the DB.
        [NotMapped]
        public decimal Value { get; set; }

        // These properties WILL be saved to the DB and must be nullable.
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
                    yield return new ValidationResult(
                        "A value greater than zero is required.",
                        new[] { nameof(Value) });
                }
            }
        }
    }
}