using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductService.Data;
using ProductService.DTOs;
using ProductService.Models;

namespace ProductService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InventoryController : ControllerBase
    {
        private readonly AppDbContext _context;
        public InventoryController(AppDbContext context) { _context = context; }

        // GET: api/inventory  - Danh sách phiếu nhập
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var receipts = await _context.StockReceipts
                .Include(r => r.Items).ThenInclude(i => i.Product)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new StockReceiptDto
                {
                    Id = r.Id,
                    ReceiptCode = r.ReceiptCode,
                    ReceiptDate = r.ReceiptDate,
                    SupplierId = r.SupplierId,
                    Note = r.Note,
                    Status = r.Status,
                    Items = r.Items.Select(i => new StockReceiptItemDto
                    {
                        ProductId = i.ProductId,
                        ProductName = i.Product!.Name,
                        Quantity = i.Quantity,
                        UnitCostPrice = i.UnitCostPrice
                    }).ToList()
                }).ToListAsync();
            return Ok(receipts);
        }

        // POST: api/inventory  - Tạo phiếu nhập kho
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateStockReceiptDto dto)
        {
            var receipt = new StockReceipt
            {
                ReceiptCode = "NK" + DateTime.Now.ToString("yyyyMMddHHmmss"),
                SupplierId = dto.SupplierId,
                Note = dto.Note,
                Status = "Pending",
                Items = dto.Items.Select(i => new StockReceiptItem
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    UnitCostPrice = i.UnitCostPrice
                }).ToList()
            };

            _context.StockReceipts.Add(receipt);
            await _context.SaveChangesAsync();
            return Ok(receipt);
        }

        // POST: api/inventory/5/confirm  - Xác nhận nhập kho → cập nhật tồn kho
        [HttpPost("{id}/confirm")]
        public async Task<IActionResult> Confirm(int id)
        {
            var receipt = await _context.StockReceipts
                .Include(r => r.Items)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (receipt == null) return NotFound();
            if (receipt.Status == "Confirmed")
                return BadRequest("Phiếu đã được xác nhận rồi");

            foreach (var item in receipt.Items)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product != null)
                    product.StockQuantity += item.Quantity;
            }

            receipt.Status = "Confirmed";
            await _context.SaveChangesAsync();

            return Ok(new { message = "Xác nhận nhập kho thành công", receiptId = id });
        }

        // GET: api/inventory/low-stock  - Sản phẩm sắp hết hàng
        [HttpGet("low-stock")]
        public async Task<IActionResult> GetLowStock()
        {
            var products = await _context.Products
                .Where(p => p.StockQuantity < p.MinStockThreshold && p.IsActive)
                .Select(p => new { p.Id, p.Code, p.Name, p.StockQuantity, p.MinStockThreshold })
                .ToListAsync();
            return Ok(products);
        }

        // GET: api/inventory/check?productId=1&quantity=5
        // Kiểm tra tính sẵn có — service khác gọi
        [HttpGet("check")]
        public async Task<IActionResult> CheckStock(
            [FromQuery] int productId,
            [FromQuery] int quantity)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
                return NotFound(new { available = false, message = "Sản phẩm không tồn tại" });

            var isAvailable = product.StockQuantity >= quantity;
            return Ok(new
            {
                productId = product.Id,
                productName = product.Name,
                stockQuantity = product.StockQuantity,
                requestedQty = quantity,
                available = isAvailable,
                message = isAvailable
                    ? $"Còn hàng ({product.StockQuantity} trong kho)"
                    : $"Không đủ hàng (chỉ còn {product.StockQuantity})"
            });
        }

        // POST: api/inventory/deduct
        // Trừ tồn kho khi đơn hàng hoàn tất — Order Service gọi
        [HttpPost("deduct")]
        public async Task<IActionResult> DeductStock([FromBody] DeductStockDto dto)
        {
            if (dto.Items == null || !dto.Items.Any())
                return BadRequest(new { message = "Danh sách sản phẩm trống" });

            // Kiểm tra tất cả trước khi trừ
            foreach (var item in dto.Items)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product == null)
                    return NotFound(new { message = $"Sản phẩm ID {item.ProductId} không tồn tại" });
                if (product.StockQuantity < item.Quantity)
                    return BadRequest(new
                    {
                        message = $"'{product.Name}' không đủ hàng. Yêu cầu: {item.Quantity}, Còn: {product.StockQuantity}"
                    });
            }

            // Trừ kho
            var result = new List<object>();
            foreach (var item in dto.Items)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                product!.StockQuantity -= item.Quantity;
                result.Add(new
                {
                    productId = product.Id,
                    productName = product.Name,
                    deducted = item.Quantity,
                    remaining = product.StockQuantity,
                    isLowStock = product.StockQuantity < product.MinStockThreshold
                });
            }

            await _context.SaveChangesAsync();
            return Ok(new
            {
                message = $"Đã trừ kho {dto.Items.Count} sản phẩm",
                orderId = dto.OrderId,
                items = result
            });
        }

        // GET: api/inventory/alerts
        // Cảnh báo hết hàng
        [HttpGet("alerts")]
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