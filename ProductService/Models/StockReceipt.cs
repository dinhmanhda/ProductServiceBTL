namespace ProductService.Models
{
    public class StockReceipt
    {
        public int Id { get; set; }
        public string ReceiptCode { get; set; } = string.Empty;
        public DateTime ReceiptDate { get; set; } = DateTime.UtcNow;
        public string? SupplierId { get; set; }    // reference ID sang service khác (KHÔNG dùng FK)
        public string? Note { get; set; }
        public string Status { get; set; } = "Pending"; // Pending / Confirmed
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<StockReceiptItem> Items { get; set; } = new List<StockReceiptItem>();
    }

    public class StockReceiptItem
    {
        public int Id { get; set; }
        public int StockReceiptId { get; set; }
        public StockReceipt? StockReceipt { get; set; }

        public int ProductId { get; set; }
        public Product? Product { get; set; }

        public int Quantity { get; set; }
        public decimal UnitCostPrice { get; set; }
    }
}