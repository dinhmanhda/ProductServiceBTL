namespace ProductService.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;       // Mã sản phẩm
        public string Name { get; set; } = string.Empty;       // Tên sản phẩm
        public decimal CostPrice { get; set; }                 // Giá nhập
        public decimal SalePrice { get; set; }                 // Giá bán
        public string? ImageUrl { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int CategoryId { get; set; }
        public Category? Category { get; set; }

        // Inventory (tồn kho)
        public int StockQuantity { get; set; } = 0;
        public int MinStockThreshold { get; set; } = 10;       // Ngưỡng cảnh báo hết hàng
    }
}