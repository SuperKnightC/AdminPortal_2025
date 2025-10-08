using AdminPortal.Data;
using AdminPortal.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Threading.Tasks;

namespace AdminPortal.Controllers
{
    public class PackageController : Controller
    {
        private readonly PackageRepository _packageRepository;
        private readonly PackageItemRepository _packageItemRepository;
        private readonly AgeCategoryRepository _ageCategoryRepository;
        private readonly AttractionRepository _attractionRepository;

        public PackageController(PackageRepository packageRepository, PackageItemRepository packageItemRepository, AgeCategoryRepository ageCategoryRepository, AttractionRepository attractionRepository)
        {
            _packageRepository = packageRepository;
            _packageItemRepository = packageItemRepository;
            _ageCategoryRepository = ageCategoryRepository;
            _attractionRepository = attractionRepository;
        }

        [HttpGet]
        public async Task<IActionResult> InsertPackage()
        {
            // Fetch ageCategory from db
            var ageCategories = await _ageCategoryRepository.GetAllAsync();
            // Fetch attraction from db
            var attractions = await _attractionRepository.GetAllActiveAsync();

            //  Create a SelectList and store it in ViewData
            ViewData["AgeCategories"] = new SelectList(ageCategories, "AgeCode", "DisplayText");
            ViewData["Attractions"] = new SelectList(attractions, "Name", "Name");


            var viewModel = new PackageViewModel();
            viewModel.Items.Add(new PackageItem());
            return View("InsertPackage", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InsertPackage(PackageViewModel model)
        {
            // If the custom validation in the models fails, return the view with the errors
            if (!ModelState.IsValid)
            {
                return View("InsertPackage", model);
            }

            try
            {
                // 1. Insert the main package and get its new ID back
                int newPackageId = await _packageRepository.InsertPackage(model);

                // 2. Loop through the items submitted from the form
                foreach (var item in model.Items)
                {
                    item.PackageID = newPackageId;

                    // This is the crucial logic to process the single 'Value' input
                    // into the correct nullable database field.
                    item.Price = null; // Reset both to null first
                    item.Point = null;

                    if (item.itemType == "Entry")
                    {
                        item.Price = item.Value;
                    }
                    else if (item.itemType == "Point" || item.itemType == "Reward")
                    {
                        item.Point = (int)item.Value; // Cast the decimal Value to an int for the Point column
                    }

                    // 3. Insert the fully processed package item
                    await _packageItemRepository.InsertPackageItem(item);
                }

                // Redirect to a success page (using "Login" as a placeholder)
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                // In case of a database error, show a generic message and log the exception
                ModelState.AddModelError("", "An unexpected error occurred while saving the package. Please try again.");
                // TODO: Log the exception 'ex' for debugging purposes
                return View("InsertPackage", model);
            }
        }
    }
}