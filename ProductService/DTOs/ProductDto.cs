namespace ProductService.DTOs
{
    public class ProductDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal CostPrice { get; set; }
        public decimal SalePrice { get; set; }
        public string? ImageUrl { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public int CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public int StockQuantity { get; set; }
        public int MinStockThreshold { get; set; }
        public bool IsLowStock => StockQuantity < MinStockThreshold;
    }

    public class CreateProductDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal CostPrice { get; set; }
        public decimal SalePrice { get; set; }
        public string? ImageUrl { get; set; }
        public string? Description { get; set; }
        public int CategoryId { get; set; }
        public int MinStockThreshold { get; set; } = 10;
    }

    public class UpdateProductDto
    {
        public string Name { get; set; } = string.Empty;
        public decimal CostPrice { get; set; }
        public decimal SalePrice { get; set; }
        public string? ImageUrl { get; set; }
        public string? Description { get; set; }
        public int CategoryId { get; set; }
        public bool IsActive { get; set; }
        public int MinStockThreshold { get; set; }
    }

    public class StockReceiptDto
    {
        public int Id { get; set; }
        public string ReceiptCode { get; set; } = string.Empty;
        public DateTime ReceiptDate { get; set; }
        public string? SupplierId { get; set; }
        public string? Note { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<StockReceiptItemDto> Items { get; set; } = new();
    }

    public class CreateStockReceiptDto
    {
        public string? SupplierId { get; set; }
        public string? Note { get; set; }
        public List<CreateStockReceiptItemDto> Items { get; set; } = new();
    }

    public class StockReceiptItemDto
    {
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitCostPrice { get; set; }
    }

    public class CreateStockReceiptItemDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitCostPrice { get; set; }
    }
}