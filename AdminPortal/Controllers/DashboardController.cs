using AdminPortal.Data;
using AdminPortal.Models;
using AdminPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

[Authorize]
[NoCache]
[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    #region -- Repository Declarations --
    private readonly PackageRepository _packageRepo;
    private readonly PackageItemRepository _packageItemRepo;
    private readonly PackageImageRepository _packageImageRepo;
    #endregion

    #region -- Constructor Injection for Repositories --
    public DashboardController(PackageRepository packageRepo, PackageItemRepository packageItemRepo, PackageImageRepository packageImageRepo)
    {
        _packageRepo = packageRepo;
        _packageItemRepo = packageItemRepo;
        _packageImageRepo = packageImageRepo;
    }
    #endregion

    #region -- All Packages Get Method --
    [HttpGet]
    public async Task<IActionResult> GetPackages([FromQuery] string status)
    {
        // Get department from JWT claims
        var department = User.Claims.FirstOrDefault(c => c.Type == "department")?.Value;

        // Security Check: Only TP department can view Draft packages
        if (status == "Draft" && department != "TP")
        {
            return Forbid(); // Returns 403 Forbidden
        }

        // Pass the status filter from the URL to your repository method
        var allPackages = await _packageRepo.GetAllAsync(status);

        // Additional security: Filter out Draft packages for non-TP users in "Show All"
        if (status == "Show All" && department != "TP")
        {
            allPackages = allPackages.Where(p => p.Status != "Draft").ToList();
        }

        var summaryList = new List<PackageSummaryViewModel>();
        var baseUrl = $"{Request.Scheme}://{Request.Host}";

        foreach (var package in allPackages)
        {
            var items = await _packageItemRepo.GetItemsByPackageIdAsync(package.PackageID);
            string imageUrl = $"{baseUrl}/images/wynsnow.jpg";

            if (!string.IsNullOrEmpty(package.ImageID) && int.TryParse(package.ImageID, out int imageId))
            {
                var specificUrl = await _packageImageRepo.GetUrlByIdAsync(imageId);
                if (!string.IsNullOrEmpty(specificUrl))
                {
                    imageUrl = $"{baseUrl}{specificUrl}";
                }
            }

            decimal totalPrice = 0;
            int totalPoints = 0;

            if (package.PackageType.Equals("Entry", StringComparison.OrdinalIgnoreCase))
            {
                totalPrice = items.Sum(item => item.Price ?? 0);
            }
            else if (package.PackageType.Equals("Point", StringComparison.OrdinalIgnoreCase) || package.PackageType.Equals("Reward", StringComparison.OrdinalIgnoreCase))
            {
                totalPoints = items.Sum(item => item.Point ?? 0);
            }


            summaryList.Add(new PackageSummaryViewModel
            {
                Id = package.PackageID,
                Name = package.Name,
                ImageUrl = imageUrl,
                PackageType = package.PackageType,
                Category = items.FirstOrDefault()?.AgeCategory ?? "N/A",
                Status = package.Status,
                Price = totalPrice,
                Point = totalPoints,
                DateCreated = package.CreatedDate,
                EntryQty = items.Sum(item => item.EntryQty) // Sum the quantity from all items
            });
        }

        return Ok(summaryList);
    }
    #endregion

    #region -- Package Detail Get Method --
    [HttpGet("{id}")] // Route: GET /api/dashboard/5
    public async Task<IActionResult> GetDetail(int id)
    {
        var packageData = await _packageRepo.GetPackageByIdAsync(id);
        if (packageData == null)
        {
            return NotFound();
        }

        // Get department from JWT claims
        var department = User.Claims.FirstOrDefault(c => c.Type == "department")?.Value;

        // Security Check: Only TP department can view Draft package details
        if (packageData.Status == "Draft" && department != "TP")
        {
            return Forbid(); // Returns 403 Forbidden
        }

        var packageItems = await _packageItemRepo.GetItemsByPackageIdAsync(id);
        var baseUrl = $"{Request.Scheme}://{Request.Host}";

        // CODE FOR HANDLING THE ImageID STRING ---
        string imageUrl = $"{baseUrl}/images/wynsnow.jpg"; // Start with a default fallback
        if (!string.IsNullOrEmpty(packageData.ImageID) && int.TryParse(packageData.ImageID, out int imageId))
        {
            var specificUrlPath = await _packageImageRepo.GetUrlByIdAsync(imageId);
            if (!string.IsNullOrEmpty(specificUrlPath))
            {
                imageUrl = $"{baseUrl}{specificUrlPath}";
            }
        }

        var calculatedEffectiveDate = packageData.LastValidDate.AddDays(-packageData.ValidDays);

        decimal totalPrice = 0;
        int totalPoints = 0;

        if (packageData.PackageType.Equals("Entry", StringComparison.OrdinalIgnoreCase))
        {
            totalPrice = packageItems.Sum(item => item.Price ?? 0);
        }
        else
        {
            totalPoints = packageItems.Sum(item => item.Point ?? 0);
        }

        var detailModel = new PackageDetailViewModel
        {
            Id = packageData.PackageID,
            Name = packageData.Name,
            PackageType = packageData.PackageType,
            TotalEntryQty = packageItems.Sum(item => item.EntryQty),
            AgeCategory = packageItems.FirstOrDefault()?.AgeCategory ?? "N/A",
            Price = totalPrice,
            Point = totalPoints,
            EffectiveDate = calculatedEffectiveDate,
            LastValidDate = packageData.LastValidDate,
            ValidDays = packageData.ValidDays,
            Status = packageData.Status,
            ImageUrl = imageUrl,
            Remark = packageData.Remark,
            CreatedDate = packageData.CreatedDate,

            // NEW: Add the submitted by and approved by fields
            SubmittedBy = packageData.CreatedByFirstName ?? "N/A",
            ApprovedBy = packageData.ModifiedByFirstName ?? "N/A",

            Items = packageItems.Select(item => new PackageItemDetail
            {
                ItemName = item.ItemName,
                Price = item.Price ?? 0,
                Point = item.Point ?? 0,
                Category = item.AgeCategory,
                EntryQty = item.EntryQty
            }).ToList()
        };

        return Ok(detailModel);
    }
    #endregion

    #region -- Approve Package Post Method --
    [HttpPost("approve/{id}")]
    public async Task<IActionResult> Approve(int id)
    {
        try
        {
            // Get current user ID
            var userIdString = User.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
            {
                return Unauthorized("User ID is not available in token.");
            }

            await _packageRepo.ApprovePackageAsync(id, userId);
            return Ok(new { message = "Package approved and transferred successfully." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"An error occurred while approving the package: {ex.Message}");
        }
    }
    #endregion

    #region -- Reject Package Post Method --
    [HttpPost("reject/{id}")]
    public async Task<IActionResult> Reject(int id)
    {
        try
        {
            // Get current user ID
            var userIdString = User.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
            {
                return Unauthorized("User ID is not available in token.");
            }

            await _packageRepo.RejectPackageAsync(id, userId);
            return Ok(new { message = "Package rejected successfully." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"An error occurred while rejecting the package: {ex.Message}");
        }
    }
    #endregion

}