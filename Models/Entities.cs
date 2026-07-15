using System.ComponentModel.DataAnnotations;
using System.Drawing;

namespace MaxVerse.API.Models;

public class Role
{
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public ICollection<User> Users { get; set; } = new List<User>();
}

public class User
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public int RoleId { get; set; }
    public Role? Role { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public bool IsActive { get; set; } = true;

    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}

public class Brand
{
    public int BrandId { get; set; }
    public string BrandName { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public ICollection<Product> Products { get; set; } = new List<Product>();
}

public class Category
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public ICollection<Product> Products { get; set; } = new List<Product>();
}

public class Product
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public decimal? DiscountPrice { get; set; }
    public int BrandId { get; set; }
    public Brand? Brand { get; set; }
    public int CategoryId { get; set; }
    public Category? Category { get; set; }
    public string? ImageUrl { get; set; }
    public int StockQuantity { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
}

public class ProductVariant
{
    [Key]
    public int VariantId { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    public string Size { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string? SKU { get; set; }
    public decimal? VariantPrice { get; set; }
    public string? VariantImageUrl { get; set; }
    public int? SizeId { get; set; }
    public Size? SizeNav { get; set; }
    public int? ColorId { get; set; }
    public MaxVerse.API.Models.Color? ColorNav { get; set; }
}

public class ProductImage
{
    [Key]
    public int ImageId { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
}

public class CartItem
{
    public int CartItemId { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public int VariantId { get; set; }
    public ProductVariant? Variant { get; set; }
    public int Quantity { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.Now;
}

public class Order
{
    public int OrderId { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string ShippingAddress { get; set; } = string.Empty;
    public string ReceiverName { get; set; } = string.Empty;
    public string ReceiverPhone { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = "COD"; // COD | VNPay
    public string PaymentStatus { get; set; } = "Pending"; // Pending | Paid | Failed
    public string OrderStatus { get; set; } = "Processing"; // Processing -> Confirmed -> Shipping -> Completed | Cancelled (từ Processing/Confirmed)
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
}

public class OrderDetail
{
    public int OrderDetailId { get; set; }
    public int OrderId { get; set; }
    public Order? Order { get; set; }
    public int VariantId { get; set; }
    public ProductVariant? Variant { get; set; }
    public string ProductNameSnapshot { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
}

public class VNPayTransaction
{
    [Key]
    public int TransactionId { get; set; }
    public int OrderId { get; set; }
    public Order? Order { get; set; }
    public string VnpTxnRef { get; set; } = string.Empty;
    public string? VnpTransactionNo { get; set; }
    public string? VnpResponseCode { get; set; }
    public decimal Amount { get; set; }
    public string? BankCode { get; set; }
    public DateTime? PayDate { get; set; }
    public string? RawResponse { get; set; }
}

public class Size
{
    public int SizeId { get; set; }
    public string SizeValue { get; set; } = string.Empty;
    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
}

public class Color
{
    public int ColorId { get; set; }
    public string ColorName { get; set; } = string.Empty;
    public string? ColorHex { get; set; }
    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
}
public class Promotion
{
    public int PromotionId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string DiscountType { get; set; } = "Percent";
    public decimal DiscountValue { get; set; }
    public decimal MinOrderAmount { get; set; }
    public int MaxUsage { get; set; }
    public int UsedCount { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; } = true;
}