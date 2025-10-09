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
        private readonly PackageImageRepository _packageImageRepository;

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
            if (!ModelState.IsValid)
            {
                // ... return view with dropdown data if validation fails
                ViewData["AgeCategories"] = new SelectList(await _ageCategoryRepository.GetAllAsync(), "AgeCode", "DisplayText");
                ViewData["Attractions"] = new SelectList(await _attractionRepository.GetAllActiveAsync(), "Name", "Name");
                return View("InsertPackage", model);
            }

            try
            {
                // 1. Process items and calculate the total price FIRST
                decimal totalPrice = 0;
                foreach (var item in model.Items)
                {
                    // Reset both properties to null first
                    item.Price = null;
                    item.Point = null;

                    // Set the correct property based on itemType and the input Value
                    if (item.itemType == "Entry")
                    {
                        item.Price = item.Value;
                        totalPrice += item.Value; // Add the item's price to the total
                    }
                    else if (item.itemType == "Point" || item.itemType == "Reward")
                    {
                        item.Point = (int)item.Value;
                    }
                }

                // 2. Overwrite the main package price with the calculated sum
                model.Price = totalPrice;

                // 3. Insert the main package with the correct, summed-up price to get its ID
                int newPackageId = await _packageRepository.InsertPackage(model);

                // 4. Now, loop through the items again to assign the new PackageID and save them
                foreach (var item in model.Items)
                {
                    item.PackageID = newPackageId;
                    await _packageItemRepository.InsertPackageItem(item);
                }

                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An unexpected error occurred while saving the package.");
                // Log the exception 'ex'
                ViewData["AgeCategories"] = new SelectList(await _ageCategoryRepository.GetAllAsync(), "AgeCode", "DisplayText");
                ViewData["Attractions"] = new SelectList(await _attractionRepository.GetAllActiveAsync(), "Name", "Name");
                return View("InsertPackage", model);
            }
        }
    }
}