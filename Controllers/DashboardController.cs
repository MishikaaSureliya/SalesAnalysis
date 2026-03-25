using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalesAnalysis.Data;
using SalesAnalysis.Models;
using System.Linq;

namespace SalesAnalysis.Controllers
{
    public class DashboardController : Controller
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var isUploaded = HttpContext.Session.GetString("DataUploaded");

            if (isUploaded == "true")
            {
                ViewBag.NoData = false;
            }
            else
            {
                ViewBag.NoData = true;
            }

            return View();
        }
    }
}