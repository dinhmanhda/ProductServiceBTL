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

            // Cập nhật tồn kho cho từng sản phẩm
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
    }
}