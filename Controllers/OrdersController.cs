using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MaxVerse.API.Data;
using MaxVerse.API.DTOs;
using MaxVerse.API.Helpers;
using MaxVerse.API.Models;

namespace MaxVerse.API.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly MaxVerseDbContext _context;
    private readonly VnPayHelper _vnPayHelper;

    public OrdersController(MaxVerseDbContext context, VnPayHelper vnPayHelper)
    {
        _context = context;
        _vnPayHelper = vnPayHelper;
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>
    /// Sơ đồ trạng thái đơn hàng hợp lệ (state machine).
    /// Processing (chờ xác nhận) -> Confirmed (đã xác nhận) -> Shipping (đang giao) -> Completed (hoàn tất)
    /// Có thể Cancelled từ Processing hoặc Confirmed (chưa giao thì còn hủy được).
    /// Completed và Cancelled là trạng thái cuối (terminal), không cho chuyển tiếp nữa.
    /// Không có đường đi ngược (vd: không thể Shipping -> Confirmed, Completed -> Shipping...).
    /// </summary>
    private static readonly Dictionary<string, string[]> OrderStatusTransitions = new()
    {
        ["Processing"] = new[] { "Confirmed", "Cancelled" },
        ["Confirmed"] = new[] { "Shipping", "Cancelled" },
        ["Shipping"] = new[] { "Completed" },
        ["Completed"] = Array.Empty<string>(),
        ["Cancelled"] = Array.Empty<string>(),
    };

    /// <summary>
    /// Tạo đơn hàng từ giỏ hàng hiện tại.
    /// Nếu PaymentMethod = "VNPay", trả về thêm paymentUrl để redirect.
    /// Nếu PaymentMethod = "COD", đơn được tạo và xác nhận luôn.
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateOrder(CreateOrderDto dto)
    {
        var cartItems = await _context.CartItems
            .Include(c => c.Variant).ThenInclude(v => v!.Product)
            .Where(c => c.UserId == CurrentUserId)
            .ToListAsync();

        if (!cartItems.Any())
            return BadRequest(new { message = "Giỏ hàng trống, không thể đặt hàng." });

        if (dto.PaymentMethod != "COD" && dto.PaymentMethod != "VNPay")
            return BadRequest(new { message = "Phương thức thanh toán không hợp lệ." });

        if (string.IsNullOrWhiteSpace(dto.ReceiverName) || string.IsNullOrWhiteSpace(dto.ReceiverPhone) || string.IsNullOrWhiteSpace(dto.ShippingAddress))
            return BadRequest(new { message = "Vui lòng nhập đầy đủ thông tin nhận hàng." });

        // Kiểm tra tồn kho trước khi tạo đơn
        foreach (var item in cartItems)
        {
            if (item.Variant!.Quantity < item.Quantity)
                return BadRequest(new { message = $"Sản phẩm '{item.Variant.Product!.ProductName}' (size {item.Variant.Size}) không đủ hàng." });
        }

        var totalAmount = cartItems.Sum(c => (c.Variant!.Product!.DiscountPrice ?? c.Variant.Product.Price) * c.Quantity);
        var orderCode = $"MV{DateTime.Now:yyyyMMddHHmmss}{CurrentUserId}";

        var order = new Order
        {
            UserId = CurrentUserId,
            OrderCode = orderCode,
            TotalAmount = totalAmount,
            ShippingAddress = dto.ShippingAddress,
            ReceiverName = dto.ReceiverName,
            ReceiverPhone = dto.ReceiverPhone,
            PaymentMethod = dto.PaymentMethod,
            PaymentStatus = dto.PaymentMethod == "COD" ? "Pending" : "Pending",
            OrderStatus = "Processing",
            OrderDetails = cartItems.Select(c => new OrderDetail
            {
                VariantId = c.VariantId,
                ProductNameSnapshot = c.Variant!.Product!.ProductName,
                UnitPrice = c.Variant.Product.DiscountPrice ?? c.Variant.Product.Price,
                Quantity = c.Quantity
            }).ToList()
        };

        // Trừ tồn kho ngay khi đặt hàng (đơn giản hóa cho đồ án; thực tế nên trừ khi thanh toán thành công)
        foreach (var item in cartItems)
        {
            item.Variant!.Quantity -= item.Quantity;
        }

        _context.Orders.Add(order);
        _context.CartItems.RemoveRange(cartItems); // Xóa giỏ hàng sau khi đặt
        await _context.SaveChangesAsync();

        if (dto.PaymentMethod == "VNPay")
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            var paymentUrl = _vnPayHelper.CreatePaymentUrl(
                orderCode: order.OrderCode,
                amount: order.TotalAmount,
                orderInfo: $"Thanh toan don hang {order.OrderCode}",
                ipAddress: ipAddress
            );

            return Ok(new { orderId = order.OrderId, orderCode = order.OrderCode, paymentUrl });
        }

        return Ok(new { orderId = order.OrderId, orderCode = order.OrderCode, message = "Đặt hàng thành công." });
    }

    /// <summary>
    /// VNPay redirect người dùng về đây sau khi thanh toán (ReturnUrl).
    /// Đây là endpoint public (không cần JWT) vì VNPay gọi trực tiếp.
    /// </summary>
    [HttpGet("vnpay-return")]
    public async Task<IActionResult> VnPayReturn()
    {
        var isValid = _vnPayHelper.ValidateSignature(Request.Query);
        if (!isValid)
            return BadRequest(new { message = "Chữ ký không hợp lệ, giao dịch có thể đã bị giả mạo." });

        var orderCode = Request.Query["vnp_TxnRef"].ToString();
        var responseCode = Request.Query["vnp_ResponseCode"].ToString();
        var transactionNo = Request.Query["vnp_TransactionNo"].ToString();
        var bankCode = Request.Query["vnp_BankCode"].ToString();
        var amountRaw = Request.Query["vnp_Amount"].ToString();

        var order = await _context.Orders
            .Include(o => o.OrderDetails).ThenInclude(d => d.Variant)
            .FirstOrDefaultAsync(o => o.OrderCode == orderCode);
        if (order == null)
            return NotFound(new { message = "Không tìm thấy đơn hàng." });

        var isSuccess = responseCode == "00";
        order.PaymentStatus = isSuccess ? "Paid" : "Failed";

        // Thanh toán thất bại/bị hủy -> hủy đơn và hoàn lại tồn kho đã trừ lúc tạo đơn
        if (!isSuccess && order.OrderStatus != "Cancelled")
        {
            order.OrderStatus = "Cancelled";
            foreach (var detail in order.OrderDetails)
            {
                if (detail.Variant != null)
                    detail.Variant.Quantity += detail.Quantity;
            }
        }

        _context.VNPayTransactions.Add(new VNPayTransaction
        {
            OrderId = order.OrderId,
            VnpTxnRef = orderCode,
            VnpTransactionNo = transactionNo,
            VnpResponseCode = responseCode,
            Amount = decimal.Parse(amountRaw) / 100,
            BankCode = bankCode,
            PayDate = DateTime.Now,
            RawResponse = Request.QueryString.ToString()
        });

        await _context.SaveChangesAsync();

        return Ok(new
        {
            success = isSuccess,
            orderCode,
            message = isSuccess ? "Thanh toán thành công." : "Thanh toán thất bại hoặc bị hủy."
        });
    }

    [HttpGet("my-orders")]
    [Authorize]
    public async Task<IActionResult> GetMyOrders()
    {
        var orders = await _context.Orders
            .Include(o => o.OrderDetails)
            .Where(o => o.UserId == CurrentUserId)
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => MapToDto(o))
            .ToListAsync();

        return Ok(orders);
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> GetOrder(int id)
    {
        var order = await _context.Orders
            .Include(o => o.OrderDetails)
            .FirstOrDefaultAsync(o => o.OrderId == id);

        if (order == null) return NotFound();

        // Chỉ chủ đơn hàng hoặc Admin mới xem được
        var isAdmin = User.IsInRole("Admin");
        if (!isAdmin && order.UserId != CurrentUserId) return Forbid();

        return Ok(MapToDto(order));
    }

    // ===== ADMIN: quản lý tất cả đơn hàng =====

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllOrders([FromQuery] string? status)
    {
        var query = _context.Orders.Include(o => o.OrderDetails).Include(o => o.User).AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(o => o.OrderStatus == status);

        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new
            {
                o.OrderId,
                o.OrderCode,
                CustomerName = o.User!.FullName,
                o.TotalAmount,
                o.PaymentMethod,
                o.PaymentStatus,
                o.OrderStatus,
                o.CreatedAt
            })
            .ToListAsync();

        return Ok(orders);
    }

    [HttpPut("{id}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateOrderStatus(int id, UpdateOrderStatusDto dto)
    {
        var order = await _context.Orders
            .Include(o => o.OrderDetails).ThenInclude(d => d.Variant)
            .FirstOrDefaultAsync(o => o.OrderId == id);
        if (order == null) return NotFound();

        if (!OrderStatusTransitions.ContainsKey(dto.OrderStatus))
            return BadRequest(new { message = "Trạng thái không hợp lệ." });

        // Không cho phép "chuyển" sang chính trạng thái hiện tại
        if (order.OrderStatus == dto.OrderStatus)
            return BadRequest(new { message = $"Đơn hàng đã ở trạng thái '{order.OrderStatus}'." });

        var allowedNext = OrderStatusTransitions.GetValueOrDefault(order.OrderStatus, Array.Empty<string>());

        // Trạng thái hiện tại là terminal (Completed/Cancelled) -> không cho đổi nữa
        if (allowedNext.Length == 0)
            return BadRequest(new { message = $"Đơn hàng đang ở trạng thái '{order.OrderStatus}', không thể thay đổi thêm." });

        // Chặn chuyển ngược / nhảy cóc không hợp lệ
        if (!allowedNext.Contains(dto.OrderStatus))
        {
            return BadRequest(new
            {
                message = $"Không thể chuyển từ '{order.OrderStatus}' sang '{dto.OrderStatus}'. " +
                           $"Các trạng thái tiếp theo hợp lệ: {string.Join(", ", allowedNext)}."
            });
        }

        var previousStatus = order.OrderStatus;
        order.OrderStatus = dto.OrderStatus;

        if (dto.OrderStatus == "Completed" && order.PaymentMethod == "COD")
        {
            order.PaymentStatus = "Paid";
        }

        // Hủy đơn -> hoàn lại tồn kho đã trừ lúc đặt hàng
        if (dto.OrderStatus == "Cancelled" && previousStatus != "Cancelled")
        {
            foreach (var detail in order.OrderDetails)
            {
                if (detail.Variant != null)
                    detail.Variant.Quantity += detail.Quantity;
            }
            if (order.PaymentStatus == "Pending")
                order.PaymentStatus = "Failed";
        }

        await _context.SaveChangesAsync();

        return Ok(new { message = "Đã cập nhật trạng thái đơn hàng.", orderStatus = order.OrderStatus });
    }

    /// <summary>
    /// Trả về danh sách trạng thái tiếp theo hợp lệ cho một đơn hàng, để frontend
    /// (vd: dropdown admin) chỉ hiển thị lựa chọn hợp lệ thay vì cho chọn tùy ý.
    /// </summary>
    [HttpGet("{id}/allowed-transitions")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllowedTransitions(int id)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order == null) return NotFound();

        var allowedNext = OrderStatusTransitions.GetValueOrDefault(order.OrderStatus, Array.Empty<string>());
        return Ok(new { currentStatus = order.OrderStatus, allowedNext });
    }
    [HttpGet("{id}/detail")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetOrderDetail(int id)
    {
        var order = await _context.Orders
            .Include(o => o.User)
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Variant)
                    .ThenInclude(v => v!.Product)
            .FirstOrDefaultAsync(o => o.OrderId == id);

        if (order == null) return NotFound();

        return Ok(new
        {
            order.OrderId,
            order.OrderCode,
            CustomerName = order.User!.FullName,
            CustomerEmail = order.User.Email,
            CustomerPhone = order.User.PhoneNumber,
            order.ReceiverName,
            order.ReceiverPhone,
            order.ShippingAddress,
            order.TotalAmount,
            order.PaymentMethod,
            order.PaymentStatus,
            order.OrderStatus,
            order.CreatedAt,
            Details = order.OrderDetails.Select(d => new
            {
                d.ProductNameSnapshot,
                Size = d.Variant?.Size,
                Color = d.Variant?.Color,
                d.UnitPrice,
                d.Quantity,
                Subtotal = d.UnitPrice * d.Quantity
            })
        });
    }

    private static OrderDto MapToDto(Order o) => new()
    {
        OrderId = o.OrderId,
        OrderCode = o.OrderCode,
        TotalAmount = o.TotalAmount,
        ReceiverName = o.ReceiverName,
        ReceiverPhone = o.ReceiverPhone,
        ShippingAddress = o.ShippingAddress,
        PaymentMethod = o.PaymentMethod,
        PaymentStatus = o.PaymentStatus,
        OrderStatus = o.OrderStatus,
        CreatedAt = o.CreatedAt,
        Details = o.OrderDetails.Select(d => new OrderDetailDto
        {
            ProductName = d.ProductNameSnapshot,
            Size = d.Variant?.Size ?? "",
            Color = d.Variant?.Color ?? "",
            UnitPrice = d.UnitPrice,
            Quantity = d.Quantity
        }).ToList()
    };
}