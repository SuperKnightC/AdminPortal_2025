using AdminPortal.Data;//call the data layer
using AdminPortal.Models; //call the model
using Microsoft.AspNetCore.Mvc; //mvc framework
using System;//exception
using System.Threading.Tasks;//async task

public class PackageController : Controller //inherit from mvc controller base class
{
    private readonly PackageRepository _packageRepository;
    private readonly PackageItemRepository _packageItemRepository; //asign the repository/object

    public PackageController(PackageRepository packageRepository,PackageItemRepository packageItemRepository)
    {
        _packageRepository = packageRepository;
        _packageItemRepository = packageItemRepository;
    }


    [HttpGet] //get method
    public IActionResult InsertPackage()
    {

        return View("InsertPackage", new PackageViewModel());//return the view with empty model
    }


    [HttpPost]//post method
    [ValidateAntiForgeryToken]//prevent cross site request forgery
    public async Task<IActionResult> InsertPackage(PackageViewModel model)//async task to handle the post request
    {
        if (!ModelState.IsValid)
        {
            return View("InsertPackage", model);
        }

        try
        {
            // 1. Insert the main package and get its new ID back
            int newPackageId = await _packageRepository.InsertPackage(model);

            // 2. Loop through the items and insert them one by one
            foreach (var item in model.Items)
            {
                item.PackageID = newPackageId;

                // Step 2: Pass the entire 'item' object to the method.
                await _packageItemRepository.InsertPackageItem(item);
            }

            return RedirectToAction("Login");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", "An error occurred while saving. Please try again.");
            // Log the exception 'ex'
            return View("InsertPackage", model);
        }
    }
}