using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using graphical_console_exporter.Models;
using Spectre.Console;

namespace graphical_console_exporter.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private static ActivitySource _source = new ActivitySource("Test");

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        using (var span = _source.StartActivity("Internal Test"))
        {
            await Task.Delay(500);
            return View();
        }
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
