using Microsoft.EntityFrameworkCore;
using ITWebManagement.Models;

namespace ITWebManagement.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Device> Devices { get; set; }
        public DbSet<Estimate> Estimates { get; set; }
        public DbSet<Purchase> Purchases { get; set; }
        public DbSet<Inventory> Inventory { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Outbound> Outbounds { get; set; } // Thêm DbSet cho Outbound

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Cấu hình kiểu dữ liệu cho Depreciation trong bảng Devices
            modelBuilder.Entity<Device>()
                .Property(d => d.Depreciation)
                .HasColumnType("decimal(18,2)");

            // Cấu hình kiểu dữ liệu cho UnitPrice và TotalPrice trong bảng Estimates
            modelBuilder.Entity<Estimate>()
                .Property(e => e.UnitPrice)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Estimate>()
                .Property(e => e.TotalPrice)
                .HasColumnType("decimal(18,2)");

            // Cấu hình kiểu dữ liệu cho UnitPrice và TotalPrice trong bảng Purchases
            modelBuilder.Entity<Purchase>()
                .Property(p => p.UnitPrice)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Purchase>()
                .Property(p => p.TotalPrice)
                .HasColumnType("decimal(18,2)");
        }
    }
}