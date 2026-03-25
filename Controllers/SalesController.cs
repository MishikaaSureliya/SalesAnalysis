using CsvHelper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalesAnalysis.Data;
using SalesAnalysis.Models;
using System.Globalization;
using System.Linq;

namespace SalesAnalysis.Controllers
{
    using SalesAnalysis.Data;
    public class SalesController : Controller
    {


private readonly AppDbContext _context;

    public SalesController(AppDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> UploadCSV(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return RedirectToAction("Index", "Dashboard", new { message = "No file selected" });

            var records = new List<Sales>();

            using (var reader = new StreamReader(file.OpenReadStream()))
            using (var csv = new CsvHelper.CsvReader(reader, System.Globalization.CultureInfo.InvariantCulture))
            {
                csv.Read();
                csv.ReadHeader();

                while (csv.Read())
                {
                    try
                    {
                        DateTime orderDate = csv.GetField<DateTime>("OrderDate");
                        string region = csv.GetField<string>("Region");
                        string category = csv.GetField<string>("Category");
                        int quantity = csv.GetField<int>("Quantity");
                        decimal price = csv.GetField<decimal>("Price");

                        if (string.IsNullOrEmpty(region) || string.IsNullOrEmpty(category))
                            continue;

                        bool exists = _context.Sales.Any(x =>
                            x.OrderDate == orderDate &&
                            x.Region == region &&
                            x.Category == category &&
                            x.Quantity == quantity &&
                            x.Price == price);

                        if (!exists)
                        {
                            var sale = new Sales
                            {
                                OrderDate = orderDate,
                                Region = region,
                                Category = category,
                                Quantity = quantity,
                                Price = price,
                                TotalSales = quantity * price,
                                Month = orderDate.Month,
                                Year = orderDate.Year
                            };

                            records.Add(sale);
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }
            }

            _context.Sales.AddRange(records);
            await _context.SaveChangesAsync();

            // CHECK DB COUNT, NOT INSERT COUNT
            var totalRecords = _context.Sales.Count();

            if (totalRecords > 0)
                HttpContext.Session.SetString("DataUploaded", "true");
            else
                HttpContext.Session.Remove("DataUploaded");

            return RedirectToAction("Index", "Dashboard", new { message = records.Count + " new records inserted" });
        }

        // ===== APIs =====

        [HttpGet("/api/sales/total-revenue")]
        public IActionResult GetTotalRevenue()
        {
            return Ok(_context.Sales.Sum(x => (decimal?)x.TotalSales) ?? 0);
        }

        [HttpGet("/api/sales/total-orders")]
        public IActionResult GetTotalOrders()
        {
            return Ok(_context.Sales.Count());
        }

        [HttpGet("/api/sales/monthly-sales")]
        public IActionResult GetMonthlySales()
        {
            var data = _context.Sales
                .GroupBy(x => x.Month)
                .Select(g => new { month = g.Key, total = g.Sum(x => x.TotalSales) })
                .ToList();

            return Ok(data);
        }

        [HttpGet("/api/sales/region-sales")]
        public IActionResult GetRegionSales()
        {
            var data = _context.Sales
                .GroupBy(x => x.Region)
                .Select(g => new { region = g.Key, total = g.Sum(x => x.TotalSales) })
                .ToList();

            return Ok(data);
        }

        [HttpGet("/api/sales/category-sales")]
        public IActionResult GetCategorySales()
        {
            var data = _context.Sales
                .GroupBy(x => x.Category)
                .Select(g => new { category = g.Key, total = g.Sum(x => x.TotalSales) })
                .ToList();

            return Ok(data);
        }

        [HttpGet("/api/sales/top-region")]
        public IActionResult GetTopRegion()
        {
            var data = _context.Sales
                .GroupBy(x => x.Region)
                .Select(g => new { region = g.Key, total = g.Sum(x => x.TotalSales) })
                .OrderByDescending(x => x.total)
                .FirstOrDefault();

            return Ok(data);
        }

        [HttpGet("/api/sales/top-category")]
        public IActionResult GetTopCategory()
        {
            var data = _context.Sales
                .GroupBy(x => x.Category)
                .Select(g => new { category = g.Key, total = g.Sum(x => x.TotalSales) })
                .OrderByDescending(x => x.total)
                .FirstOrDefault();

            return Ok(data);
        }

        [HttpGet("/api/sales/forecast")]
        public IActionResult GetForecast()
        {
            var monthly = _context.Sales
                .GroupBy(x => x.Month)
                .Select(g => new { Month = g.Key, Total = g.Sum(x => x.TotalSales) })
                .OrderBy(x => x.Month)
                .ToList();

            var last = monthly.LastOrDefault()?.Total ?? 0;

            var forecast = new List<object>
            {
                new { month = "Next 1", total = last + 200 },
                new { month = "Next 2", total = last + 400 },
                new { month = "Next 3", total = last + 600 }
            };

            return Ok(forecast);
        }
        [HttpGet("/api/sales/heatmap")]
        public IActionResult GetHeatmap(string region, string category)
        {
            var query = _context.Sales.AsQueryable();

            if (!string.IsNullOrEmpty(region))
                query = query.Where(x => x.Region == region);

            if (!string.IsNullOrEmpty(category))
                query = query.Where(x => x.Category == category);

            var data = query.ToList();

            // If not enough data, return empty matrix
            if (data.Count < 3)
            {
                return Ok(new double[][]
                {
            new double[]{1,0,0,0},
            new double[]{0,1,0,0},
            new double[]{0,0,1,0},
            new double[]{0,0,0,1}
                });
            }

            var sales = data.Select(x => (double)x.TotalSales).ToArray();
            var price = data.Select(x => (double)x.Price).ToArray();
            var quantity = data.Select(x => (double)x.Quantity).ToArray();
            var month = data.Select(x => (double)x.Month).ToArray();

            double Corr(double[] x, double[] y)
            {
                double avgX = x.Average();
                double avgY = y.Average();

                double sumXY = 0, sumX2 = 0, sumY2 = 0;

                for (int i = 0; i < x.Length; i++)
                {
                    sumXY += (x[i] - avgX) * (y[i] - avgY);
                    sumX2 += Math.Pow(x[i] - avgX, 2);
                    sumY2 += Math.Pow(y[i] - avgY, 2);
                }

                return Math.Round(sumXY / Math.Sqrt(sumX2 * sumY2), 2);
            }

            var matrix = new double[][]
            {
        new double[]{1, Corr(sales, price), Corr(sales, quantity), Corr(sales, month)},
        new double[]{Corr(price, sales), 1, Corr(price, quantity), Corr(price, month)},
        new double[]{Corr(quantity, sales), Corr(quantity, price), 1, Corr(quantity, month)},
        new double[]{Corr(month, sales), Corr(month, price), Corr(month, quantity), 1}
            };

            return Ok(matrix);
        }

        [HttpGet("filtered-data")]
        public IActionResult GetFilteredData(string region, string category)
        {
            var query = _context.Sales.AsQueryable();

            if (!string.IsNullOrEmpty(region))
                query = query.Where(x => x.Region == region);

            if (!string.IsNullOrEmpty(category))
                query = query.Where(x => x.Category == category);

            var totalRevenue = query.Sum(x => (decimal?)x.TotalSales) ?? 0;
            var totalOrders = query.Count();

            var regionSales = query.GroupBy(x => x.Region)
                .Select(g => new { region = g.Key, total = g.Sum(x => x.TotalSales) })
                .ToList();

            var categorySales = query.GroupBy(x => x.Category)
                .Select(g => new { category = g.Key, total = g.Sum(x => x.TotalSales) })
                .ToList();

            var monthlySales = query.GroupBy(x => x.Month)
                .Select(g => new { month = g.Key, total = g.Sum(x => x.TotalSales) })
                .ToList();

            var topRegion = regionSales.OrderByDescending(x => x.total).FirstOrDefault();
            var topCategory = categorySales.OrderByDescending(x => x.total).FirstOrDefault();

            // Forecast based on filtered data
            var lastMonth = monthlySales.LastOrDefault()?.total ?? 0;

            var forecast = new List<object>
    {
        new { month = "Next 1", total = lastMonth + 100 },
        new { month = "Next 2", total = lastMonth + 200 },
        new { month = "Next 3", total = lastMonth + 300 }
    };

            return Ok(new
            {
                totalRevenue,
                totalOrders,
                regionSales,
                categorySales,
                monthlySales,
                forecast,
                topRegion,
                topCategory
            });
        }
    }
}
