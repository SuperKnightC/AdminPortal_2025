using AdminPortal.Data;
using AdminPortal.Models; 
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

public class PackageController : Controller
{
    private readonly PackageRepository _packageRepository;

    public PackageController(PackageRepository packageRepository)
    {
        _packageRepository = packageRepository;
    }


    [HttpGet]
    public IActionResult InsertPackage()
    {

        return View("InsertPackage", new PackageViewModel());
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> InsertPackage(PackageViewModel model)
    {
       
        if (!ModelState.IsValid)
        {
          
            return View("InsertPackage", model);
        }

        try
        {
            await _packageRepository.InsertPackage(
                imageID: model.imageID,
                packageType: model.packageType,
                name: model.name,
                price: 0m,
                point: 0m, 
                validDay: model.validDay ?? 0, 
                dayPass: 0, 
                LastValidDate: model.LastValidDate,
                recordStatus: "A", 
                createdDate: DateTime.Now,
                createdUserID: 1, 
                modifiedDate: DateTime.Now,
                modifiedUserID: 1, 
                remarks: model.remark
            );

        
            return RedirectToAction("Login");
        }
        catch (Exception ex)
        {
          
            ModelState.AddModelError("", "An error occurred while saving the package. Please try again.");
         
            return View("InsertPackage", model);
        }
    }
}