using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductService.Data;
using ProductService.DTOs;
using ProductService.Models;

namespace ProductService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProductsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/products
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetAll(
            [FromQuery] string? search,
            [FromQuery] int? categoryId,
            [FromQuery] bool? lowStock)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive) // Ẩn sản phẩm đã bị xóa mềm
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
                query = query.Where(p => p.Name.Contains(search) || p.Code.Contains(search));

            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId.Value);

            if (lowStock == true)
                query = query.Where(p => p.StockQuantity < p.MinStockThreshold);

            var products = await query.Select(p => new ProductDto
            {
                Id = p.Id,
                Code = p.Code,
                Name = p.Name,
                CostPrice = p.CostPrice,
                SalePrice = p.SalePrice,
                ImageUrl = p.ImageUrl,
                Description = p.Description,
                IsActive = p.IsActive,
                CategoryId = p.CategoryId,
                CategoryName = p.Category!.Name,
                StockQuantity = p.StockQuantity,
                MinStockThreshold = p.MinStockThreshold
            }).ToListAsync();

            return Ok(products);
        }

        // GET: api/products/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ProductDto>> GetById(int id)
        {
            var p = await _context.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == id);
            if (p == null) return NotFound();

            return Ok(new ProductDto
            {
                Id = p.Id,
                Code = p.Code,
                Name = p.Name,
                CostPrice = p.CostPrice,
                SalePrice = p.SalePrice,
                ImageUrl = p.ImageUrl,
                Description = p.Description,
                IsActive = p.IsActive,
                CategoryId = p.CategoryId,
                CategoryName = p.Category?.Name,
                StockQuantity = p.StockQuantity,
                MinStockThreshold = p.MinStockThreshold
            });
        }

        // GET: api/products/5/price  ← Order Service gọi API này để lấy giá
        [HttpGet("{id}/price")]
        public async Task<ActionResult> GetPrice(int id)
        {
            var p = await _context.Products.FindAsync(id);
            if (p == null) return NotFound();
            return Ok(new { productId = p.Id, salePrice = p.SalePrice, name = p.Name, stock = p.StockQuantity });
        }

        // POST: api/products
        [HttpPost]
        public async Task<ActionResult<ProductDto>> Create([FromBody] CreateProductDto dto)
        {
            var product = new Product
            {
                Code = dto.Code,
                Name = dto.Name,
                CostPrice = dto.CostPrice,
                SalePrice = dto.SalePrice,
                ImageUrl = dto.ImageUrl,
                Description = dto.Description,
                CategoryId = dto.CategoryId,
                MinStockThreshold = dto.MinStockThreshold
            };
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
        }

        // PUT: api/products/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateProductDto dto)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            product.Name = dto.Name;
            product.CostPrice = dto.CostPrice;
            product.SalePrice = dto.SalePrice;
            product.ImageUrl = dto.ImageUrl;
            product.Description = dto.Description;
            product.CategoryId = dto.CategoryId;
            product.IsActive = dto.IsActive;
            product.MinStockThreshold = dto.MinStockThreshold;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/products/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            // Kiểm tra sản phẩm có đang trong phiếu nhập kho không
            var hasReceipt = await _context.StockReceiptItems
                .AnyAsync(i => i.ProductId == id);

            if (hasReceipt)
            {
                // Soft delete: đánh dấu không hoạt động thay vì xóa cứng
                // để bảo toàn lịch sử nhập kho
                product.IsActive = false;
                await _context.SaveChangesAsync();
                return NoContent();
            }

            // Không có lịch sử nhập kho → xóa cứng hoàn toàn
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // PATCH: api/products/5/stock  ← Order Service gọi khi bán hàng để trừ tồn kho
        [HttpPatch("{id}/stock")]
        public async Task<IActionResult> UpdateStock(int id, [FromBody] UpdateStockDto dto)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            product.StockQuantity += dto.QuantityChange; // âm = trừ, dương = cộng
            if (product.StockQuantity < 0) product.StockQuantity = 0;

            await _context.SaveChangesAsync();
            return Ok(new { productId = id, newStock = product.StockQuantity });
        }
    }

    public class UpdateStockDto
    {
        public int QuantityChange { get; set; } // -5 = trừ 5, +10 = cộng 10
    }
}