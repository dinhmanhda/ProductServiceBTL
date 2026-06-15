using Microsoft.EntityFrameworkCore;
using ProductService.Models;

namespace ProductService.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<StockReceipt> StockReceipts { get; set; }
        public DbSet<StockReceiptItem> StockReceiptItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Self-referencing category
            modelBuilder.Entity<Category>()
                .HasOne(c => c.ParentCategory)
                .WithMany(c => c.SubCategories)
                .HasForeignKey(c => c.ParentCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // ✅ Dùng numeric thay decimal(18,2) cho PostgreSQL
            modelBuilder.Entity<Product>()
                .Property(p => p.CostPrice)
                .HasColumnType("numeric(18,2)");

            modelBuilder.Entity<Product>()
                .Property(p => p.SalePrice)
                .HasColumnType("numeric(18,2)");

            modelBuilder.Entity<StockReceiptItem>()
                .Property(p => p.UnitCostPrice)
                .HasColumnType("numeric(18,2)");

            // Seed dữ liệu mẫu
            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Điện tử", Description = "Thiết bị điện tử" },
                new Category { Id = 2, Name = "Thực phẩm", Description = "Đồ ăn uống" }
            );
        }
    }
}