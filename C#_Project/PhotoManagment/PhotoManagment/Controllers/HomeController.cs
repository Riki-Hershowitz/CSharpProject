using Microsoft.AspNetCore.Mvc;
using PhotoManagment.Models;
using System.Diagnostics;
using BLL.Services;
using BLL.Interfaces;

namespace PhotoManagment.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IMemoryCardService _memoryCardService;

        public HomeController(ILogger<HomeController> logger, IMemoryCardService memoryCardService)
        {
            _logger = logger;
            _memoryCardService = memoryCardService;
        }

        public IActionResult Index()
        {
            _memoryCardService.ProcessMemoryCard();
            return View();
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
}
