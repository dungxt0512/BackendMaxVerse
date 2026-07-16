namespace MaxVerse.API.DTOs;

public class AddCartItemDto
{
    public int VariantId { get; set; }
    public int Quantity { get; set; } = 1;
}

public class UpdateCartItemDto
{
    public int Quantity { get; set; }
}

public class CartItemDto
{
    public int CartItemId { get; set; }
    public int VariantId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string Size { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public int AvailableStock { get; set; }
    public decimal Subtotal => UnitPrice * Quantity;
}

public class CreateOrderDto
{
    public string ReceiverName { get; set; } = string.Empty;
    public string ReceiverPhone { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = "COD"; // "COD" hoặc "VNPay"
}

public class OrderDto
{
    public int OrderId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string ReceiverName { get; set; } = string.Empty;
    public string ReceiverPhone { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public string OrderStatus { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<OrderDetailDto> Details { get; set; } = new();
}

public class OrderDetailDto
{
    public string ProductName { get; set; } = string.Empty;
    public string Size { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
}

public class UpdateOrderStatusDto
{
    public string OrderStatus { get; set; } = string.Empty;
}

public class CreatePosOrderDto
{
    public string? ReceiverName { get; set; }
    public string? ReceiverPhone { get; set; }
    public string PaymentMethod { get; set; } = "Cash";
    public List<PosOrderItemDto> Items { get; set; } = new();
}

public class PosOrderItemDto
{
    public int VariantId { get; set; }
    public int Quantity { get; set; }
}