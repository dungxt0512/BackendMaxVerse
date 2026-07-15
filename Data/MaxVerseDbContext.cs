using Microsoft.EntityFrameworkCore;
using MaxVerse.API.Models;

namespace MaxVerse.API.Data;

public class MaxVerseDbContext : DbContext
{
    public MaxVerseDbContext(DbContextOptions<MaxVerseDbContext> options) : base(options) { }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderDetail> OrderDetails => Set<OrderDetail>();
    public DbSet<VNPayTransaction> VNPayTransactions => Set<VNPayTransaction>();
    public DbSet<Size> Sizes => Set<Size>();
    public DbSet<Color> Colors => Set<Color>();
    public DbSet<Promotion> Promotions => Set<Promotion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>().Property(p => p.Price).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Product>().Property(p => p.DiscountPrice).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Order>().Property(o => o.TotalAmount).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<OrderDetail>().Property(o => o.UnitPrice).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<VNPayTransaction>().Property(v => v.Amount).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<ProductVariant>().Property(v => v.VariantPrice).HasColumnType("decimal(18,2)");

        modelBuilder.Entity<ProductVariant>()
            .HasOne(v => v.SizeNav)
            .WithMany(s => s.Variants)
            .HasForeignKey(v => v.SizeId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<ProductVariant>()
            .HasOne(v => v.ColorNav)
            .WithMany(c => c.Variants)
            .HasForeignKey(v => v.ColorId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();
        modelBuilder.Entity<Order>().HasIndex(o => o.OrderCode).IsUnique();
        modelBuilder.Entity<Brand>().HasIndex(b => b.BrandName).IsUnique();
        modelBuilder.Entity<Category>().HasIndex(c => c.CategoryName).IsUnique();

        modelBuilder.Entity<Order>()
            .HasOne(o => o.User)
            .WithMany(u => u.Orders)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<OrderDetail>()
            .HasOne(od => od.Variant)
            .WithMany()
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CartItem>()
            .HasOne(c => c.Variant)
            .WithMany()
            .OnDelete(DeleteBehavior.Restrict);
    }
}
