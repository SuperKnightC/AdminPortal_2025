using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// Assuming your models are in this namespace
using AdminPortal.Models;

public class FinanceController : Controller
{
    // This action shows the list of all packages
    public IActionResult Index()
    {
        // In a real application, you would fetch data from your PackageRepository here.
        var allPackages = _getMockPackageData();

        var summaryList = allPackages.Select(p => new PackageSummaryViewModel
        {
            Id = p.Id,
            Name = p.Name,
            ImageUrl = p.ImageUrl,
            PackageType = p.PackageType,
            Quantity = p.Items.Count,
            Category = p.Items.FirstOrDefault()?.Category ?? "N/A"
        }).ToList();

        return View(summaryList);
    }

    // This action shows the details for a single package
    public IActionResult Detail(int id)
    {
        // In a real application, you would fetch a single package by its ID.
        var package = _getMockPackageData().FirstOrDefault(p => p.Id == id);

        if (package == null)
        {
            return NotFound();
        }

        return View(package);
    }

    // This private method simulates fetching data from a database
    private List<PackageDetailViewModel> _getMockPackageData()
    {
        return new List<PackageDetailViewModel>
        {
            new PackageDetailViewModel
            {
                Id = 1, Name = "City of Digital Lights Zone A", ImageUrl = "https://cdn.pixabay.com/photo/2022/07/14/06/41/amusement-park-7320164_1280.jpg",
                PackageType = "Bundle", Status = "Pending", EffectiveDate = DateTime.Now, LastValidDate = DateTime.Now.AddDays(30), ValidDays = 30,
                Items = new List<PackageItemDetail> { new PackageItemDetail { ItemName = "Disco Ride", Price = 5, Category = "Malaysian Adult" } }
            },
            new PackageDetailViewModel
            {
                Id = 2, Name = "City of Digital Lights Zone B", ImageUrl = "https://cdn.pixabay.com/photo/2018/07/12/21/26/ferris-wheel-3538713_1280.jpg",
                PackageType = "Bundle", Status = "Approved", EffectiveDate = DateTime.Now, LastValidDate = DateTime.Now.AddDays(60), ValidDays = 60,
                Items = new List<PackageItemDetail> { new PackageItemDetail { ItemName = "Space Walk", Price = 10, Category = "Malaysian Child" }, new PackageItemDetail { ItemName = "Super Swing", Price = 5, Category = "Malaysian Child" } }
            },
        };
    }
}