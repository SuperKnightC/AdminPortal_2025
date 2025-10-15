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
    [HttpGet] // Route: GET /api/dashboard
    public async Task<IActionResult> GetPackages([FromQuery] string status)
    {
        var allPackages = await _packageRepo.GetAllAsync(status);
        var summaryList = new List<PackageSummaryViewModel>();

        foreach (var package in allPackages)
        {
            var items = await _packageItemRepo.GetItemsByPackageIdAsync(package.PackageID);

            // --- CORRECTED CODE FOR HANDLING THE ImageID STRING ---
            string imageUrl = "https://localhost:7029/images/gun.jpg"; 

            // Check if the ImageID string is not null/empty and can be converted to a number
            if (!string.IsNullOrEmpty(package.ImageID) && int.TryParse(package.ImageID, out int imageId))
            {
                // If it's valid, fetch the real URL from the database
                imageUrl = await _packageImageRepo.GetUrlByIdAsync(imageId) ?? imageUrl;
            }

            summaryList.Add(new PackageSummaryViewModel
            {
                Id = package.PackageID,
                Name = package.Name,
                ImageUrl = imageUrl, // Use the imageUrl we determined above
                PackageType = package.PackageType,
                Quantity = items.Count,
                Category = items.FirstOrDefault()?.AgeCategory ?? "N/A",
                Status = package.Status
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

        var packageItems = await _packageItemRepo.GetItemsByPackageIdAsync(id);

        // CODE FOR HANDLING THE ImageID STRING ---
        string imageUrl = "https://localhost:7029/images/gun.jpg"; // Start with a default fallback
        if (!string.IsNullOrEmpty(packageData.ImageID) && int.TryParse(packageData.ImageID, out int imageId))
        {
            imageUrl = await _packageImageRepo.GetUrlByIdAsync(imageId) ?? imageUrl;
        }

        var calculatedEffectiveDate = packageData.LastValidDate.AddDays(-packageData.ValidDays);

        var detailModel = new PackageDetailViewModel
        {
            Id = packageData.PackageID,
            Name = packageData.Name,
            PackageType = packageData.PackageType,
            EffectiveDate = calculatedEffectiveDate,
            LastValidDate = packageData.LastValidDate,
            ValidDays = packageData.ValidDays,
            Status = packageData.Status,
            ImageUrl = imageUrl, // Use the imageUrl we determined above
            Items = packageItems.Select(item => new PackageItemDetail
            {
                ItemName = item.ItemName,
                Price = item.Price ?? 0,
                Point = item.Point ?? 0,
                Category = item.AgeCategory
            }).ToList()
        };

        return Ok(detailModel);
    }
    #endregion

    #region -- Approve Package Post Method --
    [HttpPost("approve/{id}")] // Handles POST requests to /api/Dashboard/approve/{id}
    public async Task<IActionResult> Approve(int id)
    {
        try
        {
            await _packageRepo.ApprovePackageAsync(id);
            return Ok(new { message = "Package approved and transferred successfully." });
        }
        catch (Exception ex)
        {
            // Log the exception ex
            return StatusCode(500, $"An error occurred while approving the package: {ex.Message}");
        }
    }
    #endregion

    #region -- Reject Package Post Method --
    [HttpPost("reject/{id}")] // Handles POST requests to /api/Dashboard/reject/{id}
    public async Task<IActionResult> Reject(int id)
    {
        try
        {
            await _packageRepo.RejectPackageAsync(id);
            return Ok(new { message = "Package rejected successfully." });
        }
        catch (Exception ex)
        {
            // Log the exception ex
            return StatusCode(500, $"An error occurred while rejecting the package: {ex.Message}");
        }
    }
    #endregion




}