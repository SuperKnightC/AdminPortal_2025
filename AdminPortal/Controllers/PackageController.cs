using AdminPortal.Data;
using AdminPortal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class PackageController : ControllerBase
{
    #region -- Repository Declarations --
    private readonly PackageRepository _packageRepository;
    private readonly PackageItemRepository _packageItemRepository;
    private readonly AgeCategoryRepository _ageCategoryRepository;
    private readonly AttractionRepository _attractionRepository;
    private readonly PackageImageRepository _packageImageRepository;
    #endregion

    #region -- Constructor Injection for Repositories --
    public PackageController(PackageRepository packageRepository,
           PackageItemRepository packageItemRepository,
           AgeCategoryRepository ageCategoryRepository,
           AttractionRepository attractionRepository,
           PackageImageRepository packageImageRepository)
    {
        _packageRepository = packageRepository;
        _packageItemRepository = packageItemRepository;
        _ageCategoryRepository = ageCategoryRepository;
        _attractionRepository = attractionRepository;
        _packageImageRepository = packageImageRepository;
    }
    #endregion

    #region -- Package Age & Attraction Get Method --
    // This endpoint provides the necessary data to build the "Insert Package" form on the front-end
    [HttpGet("creationdata")] // Route: GET /api/package/creationdata
    public async Task<IActionResult> GetPackageCreationData()
    {
        var ageCategories = await _ageCategoryRepository.GetAllAsync();
        var attractions = await _attractionRepository.GetAllActiveAsync();

        var data = new
        {
            AgeCategories = ageCategories,
            Attractions = attractions
        };

        return Ok(data);
    }
    #endregion

    #region -- Image Upload Post Method --
    [HttpPost("upload")]
    [Authorize(Policy = "CanCreatePackage")]
    public async Task<IActionResult> UploadImage(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded.");

        // In a real app, you'd save to Azure Blob, S3, etc.
        // For now, we save to wwwroot/images/packages
        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "packages");
        if (!Directory.Exists(uploadsFolder))
            Directory.CreateDirectory(uploadsFolder);

        var uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // The URL path to be saved in the database
        var imageUrl = $"/images/packages/{uniqueFileName}";
        var imageId = await _packageImageRepository.InsertAsync(imageUrl);

        return Ok(new { imageId = imageId.ToString(), imageUrl = imageUrl });
    }
    #endregion

    #region -- Insert Package Post Method --
    [HttpPost("create")]
    [Authorize(Policy = "CanCreatePackage")]
    public async Task<IActionResult> InsertPackage([FromBody] PackageViewModel model)
    {
        if (!ModelState.IsValid)
        {
            // Return a 400 Bad Request with the validation errors as JSON
            return BadRequest(ModelState);
        }

        try
        {
            // --- GET CURRENT USER ID ---
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
            {
                return Unauthorized("User ID is not available in token.");
            }
            // This core logic remains exactly the same!
            decimal totalPrice = 0;
            int totalPoints = 0;

            foreach (var item in model.Items)
            {
                item.Price = null;
                item.Point = null;
                if (item.itemType == "Entry")
                {
                    item.Price = item.Value;
                    totalPrice += item.Value * item.EntryQty;
                }
                else if (item.itemType == "Point" || item.itemType == "Reward")
                {
                    item.Point = (int)item.Value;
                    totalPoints += (int)item.Value * item.EntryQty;
                }
            }

            if (model.packageType == "Entry")
            {
                model.Price = totalPrice;
                model.Point = 0;
            }
            else if (model.packageType == "Point" || model.packageType == "Reward")
            {
                model.Price = 0;
                model.Point = totalPoints;
            }

            int newPackageId = await _packageRepository.InsertPackage(model,userId);

            foreach (var item in model.Items)
            {
                item.PackageID = newPackageId;
                await _packageItemRepository.InsertPackageItem(item,userId);
            }

            return Ok(new { message = "Package created successfully", packageId = newPackageId });
        }
        catch (Exception ex)
        {
            // In case of an error, return a 500 Internal Server Error status
            // Log the exception 'ex'
            return StatusCode(500, "An internal server error occurred.");
        }
    }
    #endregion

    #region -- Finance Approve Reject Put Method --
    [HttpPut("{id}/status")]
    [Authorize(Policy = "FinanceOnly")] //  policy for FN users
    public async Task<IActionResult> UpdatePackageStatus(int id, [FromBody] StatusUpdateModel model)
    {
        if (model == null || (model.Status != "Approved" && model.Status != "Rejected"))
        {
            return BadRequest("Invalid status provided.");
        }

        // --- GET CURRENT USER ID ---
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
        {
            return Unauthorized("User ID is not available in token.");
        }

        var success = await _packageRepository.UpdatePackageStatusAsync(id, model.Status,userId);

        if (!success)
        {
            return NotFound(new { message = "Package not found or status could not be updated." });
        }

        return Ok(new { message = $"Package status updated to {model.Status}" });
    }
    #endregion
}

