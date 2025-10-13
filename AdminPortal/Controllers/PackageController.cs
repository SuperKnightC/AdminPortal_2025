using AdminPortal.Data;
using AdminPortal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    #endregion

    #region -- Constructor Injection for Repositories --
    public PackageController(PackageRepository packageRepository,
           PackageItemRepository packageItemRepository,
           AgeCategoryRepository ageCategoryRepository,
           AttractionRepository attractionRepository)
    {
        _packageRepository = packageRepository;
        _packageItemRepository = packageItemRepository;
        _ageCategoryRepository = ageCategoryRepository;
        _attractionRepository = attractionRepository;
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

    #region -- Insert Package Post Method --
    [HttpPost]
    public async Task<IActionResult> InsertPackage([FromBody] PackageViewModel model)
    {
        if (!ModelState.IsValid)
        {
            // Return a 400 Bad Request with the validation errors as JSON
            return BadRequest(ModelState);
        }

        try
        {
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
                    totalPrice += item.Value;
                }
                else if (item.itemType == "Point" || item.itemType == "Reward")
                {
                    item.Point = (int)item.Value;
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

            int newPackageId = await _packageRepository.InsertPackage(model);

            foreach (var item in model.Items)
            {
                item.PackageID = newPackageId;
                await _packageItemRepository.InsertPackageItem(item);
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

}