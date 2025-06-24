using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using HealthCheckDemo.Models;

namespace HealthCheckDemo.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Health()
    {
        return View();
    }
}
