using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductService.Data;

namespace ProductService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AlertsController : ControllerBase
    {
        private readonly AppDbContext _context;
        public AlertsController(AppDbContext context) { _context = context; }

        // GET: api/alerts
        [HttpGet]
        public async Task<IActionResult> GetAlerts()
        {
            var products = await _context.Products
                .Where(p => p.IsActive)
                .Select(p => new
                {
                    p.Id,
                    p.Code,
                    p.Name,
                    p.StockQuantity,
                    p.MinStockThreshold,
                    status = p.StockQuantity == 0 ? "out_of_stock"
                           : p.StockQuantity < p.MinStockThreshold ? "low_stock"
                           : "ok"
                })
                .Where(p => p.status != "ok")
                .ToListAsync();

            return Ok(new
            {
                total = products.Count,
                outOfStock = products.Count(p => p.status == "out_of_stock"),
                lowStock = products.Count(p => p.status == "low_stock"),
                items = products
            });
        }
    }
}