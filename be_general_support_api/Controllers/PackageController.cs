using be_general_support_api.Data;
using be_general_support_api.Models;
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
    // Route: GET /api/package/creationdata
    // This endpoint provides the necessary data to build the "Insert Package" form on the front-end
    [HttpGet("creationdata")] 
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
    // Route: POST /api/package/upload
    //This endpoint handles image uploads for packages
    [HttpPost("upload")]
    [Authorize(Policy = "CanCreatePackage")]
    public async Task<IActionResult> UploadImage(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded.");

        // save to wwwroot/images/packages
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
    // Route: POST /api/package/create
    // This endpoint handles the creation of a new package along with its items
    [HttpPost("create")]
    [Authorize(Policy = "CanCreatePackage")]
    public async Task<IActionResult> InsertPackage([FromBody] PackageViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            // --- GET CURRENT USER ID FROM CORRECT CLAIM ---
            var userIdString = User.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
            {
                return Unauthorized("User ID is not available in token.");
            }

            decimal totalPrice = 0;
            int totalPoints = 0;

            foreach (var item in model.Items)
            {
                item.Nationality = model.Nationality;
                item.AgeCategory = model.AgeCategory;
                item.Price = null;
                item.Point = null;

                if (item.itemType == "Entry")
                {
                    item.Price = item.Value;
                    // OPTION A: Just sum the base prices (NO multiplication)
                    totalPrice += item.Value;
                }
                else if (item.itemType == "Point" || item.itemType == "Reward")
                {
                    item.Point = (int)item.Value;
                    // OPTION A: Just sum the base points (NO multiplication)
                    totalPoints += (int)item.Value;
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

            int newPackageId = await _packageRepository.InsertPackage(model, userId);

            foreach (var item in model.Items)
            {
                item.PackageID = newPackageId;
                await _packageItemRepository.InsertPackageItem(item, userId);
            }

            return Ok(new { message = "Package created successfully", packageId = newPackageId });
        }
        catch (Exception ex)
        {
            // Log the exception
            return StatusCode(500, new { message = $"An internal server error occurred: {ex.Message}" });
        }
    }
    #endregion

    #region -- Finance Approve Reject Put Method --
    // Route: PUT /api/package/{id}/status
    // This endpoint allows finance users to approve or reject packages
    [HttpPut("{id}/status")]
    [Authorize(Policy = "FinanceOnly")] //  policy for FN users
    public async Task<IActionResult> UpdatePackageStatus(int id, [FromBody] StatusUpdateModel model)
    {
        if (model == null || string.IsNullOrEmpty(model.Status))
        {
            return BadRequest("Invalid status provided.");
        }

        // --- FIXED: GET CURRENT USER ID FROM CORRECT CLAIM ---
        var userIdString = User.FindFirst("userId")?.Value;
        if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
        {
            return Unauthorized("User ID is not available in token.");
        }

        try
        {
            // --- LOGIC FIX: Call the correct repository methods ---
            if (model.Status == "Approved")
            {
                // Use the method that copies to App_PackageAO tables
                await _packageRepository.ApprovePackageAsync(id, userId);
                return Ok(new { message = "Package approved and transferred successfully." });
            }
            else if (model.Status == "Rejected")
            {
                // --- THIS IS THE FIX ---
                // Use the dedicated reject method, passing the remark
                await _packageRepository.RejectPackageAsync(id, userId, model.FinanceRemark);
                return Ok(new { message = "Package rejected successfully." });
            }
            else
            {
                // Only use the simple status update for other statuses (if any)
                var success = await _packageRepository.UpdatePackageStatusAsync(id, model.Status, userId);
                if (!success)
                {
                    return NotFound(new { message = "Package not found or status could not be updated." });
                }
                return Ok(new { message = $"Package status updated to {model.Status}" });
            }
        }
        catch (Exception ex)
        {
            // BREAKPOINT 7 ⭐ - Check if exception is thrown
            Console.WriteLine($"!!! CONTROLLER ERROR: {ex.Message}");
            Console.WriteLine($"!!! Stack Trace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"!!! Inner Exception: {ex.InnerException.Message}");
            }
            return StatusCode(500, new
            {
                message = $"An internal server error occurred: {ex.Message}",
                stackTrace = ex.StackTrace
            });
        }
    }
    #endregion

    #region -- Duplicate Package Post Method --
    // Route: POST /api/package/{id}/duplicate
    // This endpoint allows users to duplicate an existing package as a Draft
    [HttpPost("{id}/duplicate")]
    [Authorize(Policy = "CanCreatePackage")] // Only users who can create can duplicate
    public async Task<IActionResult> DuplicatePackage(int id)
    {
        try
        {
            // Get current user ID from the token
            var userIdString = User.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
            {
                return Unauthorized("User ID is not available in token.");
            }

            // Call the new repository method
            int newPackageId = await _packageRepository.DuplicatePackageAsync(id, userId);

            return Ok(new { message = "Package duplicated successfully as Draft.", newPackageId = newPackageId });
        }
        catch (Exception ex)
        {
            // Log the exception
            return StatusCode(500, new { message = $"An internal server error occurred: {ex.Message}" });
        }
    }
    #endregion

}