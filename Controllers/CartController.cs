using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MaxVerse.API.Data;
using MaxVerse.API.DTOs;
using MaxVerse.API.Models;

namespace MaxVerse.API.Controllers;

[ApiController]
[Route("api/cart")]
[Authorize] // Bắt buộc đăng nhập mới được dùng giỏ hàng (lưu theo UserId)
public class CartController : ControllerBase
{
    private readonly MaxVerseDbContext _context;

    public CartController(MaxVerseDbContext context)
    {
        _context = context;
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetCart()
    {
        var items = await _context.CartItems
            .Include(c => c.Variant).ThenInclude(v => v!.Product)
            .Where(c => c.UserId == CurrentUserId)
            .Select(c => new CartItemDto
            {
                CartItemId = c.CartItemId,
                VariantId = c.VariantId,
                ProductId = c.Variant!.ProductId,
                ProductName = c.Variant.Product!.ProductName,
                ImageUrl = c.Variant.Product.ImageUrl,
                Size = c.Variant.Size,
                Color = c.Variant.Color,
                UnitPrice = c.Variant.Product.DiscountPrice ?? c.Variant.Product.Price,
                Quantity = c.Quantity,
                AvailableStock = c.Variant.Quantity
            })
            .ToListAsync();

        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> AddToCart(AddCartItemDto dto)
    {
        var variant = await _context.ProductVariants.FindAsync(dto.VariantId);
        if (variant == null) return NotFound(new { message = "Không tìm thấy biến thể sản phẩm." });

        if (dto.Quantity <= 0)
            return BadRequest(new { message = "Số lượng phải lớn hơn 0." });

        var existing = await _context.CartItems
            .FirstOrDefaultAsync(c => c.UserId == CurrentUserId && c.VariantId == dto.VariantId);

        if (existing != null)
        {
            existing.Quantity += dto.Quantity;
        }
        else
        {
            _context.CartItems.Add(new CartItem
            {
                UserId = CurrentUserId,
                VariantId = dto.VariantId,
                Quantity = dto.Quantity
            });
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = "Đã thêm vào giỏ hàng." });
    }

    [HttpPut("{cartItemId}")]
    public async Task<IActionResult> UpdateCartItem(int cartItemId, UpdateCartItemDto dto)
    {
        var item = await _context.CartItems
            .FirstOrDefaultAsync(c => c.CartItemId == cartItemId && c.UserId == CurrentUserId);

        if (item == null) return NotFound();

        if (dto.Quantity <= 0)
            return BadRequest(new { message = "Số lượng phải lớn hơn 0." });

        item.Quantity = dto.Quantity;
        await _context.SaveChangesAsync();
        return Ok(new { message = "Đã cập nhật số lượng." });
    }

    [HttpDelete("{cartItemId}")]
    public async Task<IActionResult> RemoveCartItem(int cartItemId)
    {
        var item = await _context.CartItems
            .FirstOrDefaultAsync(c => c.CartItemId == cartItemId && c.UserId == CurrentUserId);

        if (item == null) return NotFound();

        _context.CartItems.Remove(item);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Đã xóa sản phẩm khỏi giỏ hàng." });
    }
}
