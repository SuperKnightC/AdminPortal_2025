
namespace AdminPortal.Models
{
    public class AgeCategory
    {
        public string AgeCode { get; set; }
        public string CategoryName { get; set; }

        // This read-only property creates the "Code - Name" format for the dropdown
        public string DisplayText => $"{AgeCode} - {CategoryName}";
    }
}