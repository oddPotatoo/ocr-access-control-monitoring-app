using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OCR_AccessControl.Models;

namespace OCR_AccessControl.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    [AllowAnonymous] // 🔹 Ensure login page is accessible to everyone
    public IActionResult Index()
    {
        return View(); // 🔹 This must match /Views/Home/Index.cshtml
    }

    public IActionResult AccessDenied()
    {
        return View(); // 🔹 Ensure /Views/Home/AccessDenied.cshtml exists
    }


    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
