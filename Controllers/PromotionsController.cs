using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MaxVerse.API.Data;
using MaxVerse.API.Models;

namespace MaxVerse.API.Controllers;

[ApiController]
[Route("api/promotions")]
public class PromotionsController : ControllerBase
{
    private readonly MaxVerseDbContext _context;
    public PromotionsController(MaxVerseDbContext context) => _context = context;

    // Khách hàng áp dụng mã giảm giá
    [HttpPost("apply")]
    [Authorize]
    public async Task<IActionResult> ApplyPromotion([FromBody] ApplyPromotionDto dto)
    {
        var promo = await _context.Promotions
            .FirstOrDefaultAsync(p => p.Code == dto.Code.ToUpper() && p.IsActive);

        if (promo == null)
            return BadRequest(new { message = "Mã giảm giá không tồn tại hoặc đã hết hạn." });

        if (promo.UsedCount >= promo.MaxUsage)
            return BadRequest(new { message = "Mã giảm giá đã được sử dụng hết." });

        if (DateTime.Now < promo.StartDate || DateTime.Now > promo.EndDate)
            return BadRequest(new { message = "Mã giảm giá chưa có hiệu lực hoặc đã hết hạn." });

        if (dto.OrderAmount < promo.MinOrderAmount)
            return BadRequest(new { message = $"Đơn hàng cần tối thiểu {promo.MinOrderAmount:N0}đ để áp dụng mã này." });

        decimal discountAmount = promo.DiscountType == "Percent"
            ? dto.OrderAmount * promo.DiscountValue / 100
            : promo.DiscountValue;

        return Ok(new
        {
            code = promo.Code,
            description = promo.Description,
            discountType = promo.DiscountType,
            discountValue = promo.DiscountValue,
            discountAmount
        });
    }

    // Admin CRUD khuyến mãi
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll()
    {
        var promos = await _context.Promotions.OrderByDescending(p => p.PromotionId).ToListAsync();
        return Ok(promos);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(Promotion dto)
    {
        dto.Code = dto.Code.ToUpper();
        _context.Promotions.Add(dto);
        await _context.SaveChangesAsync();
        return Ok(dto);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, Promotion dto)
    {
        var promo = await _context.Promotions.FindAsync(id);
        if (promo == null) return NotFound();

        promo.Code = dto.Code.ToUpper();
        promo.Description = dto.Description;
        promo.DiscountType = dto.DiscountType;
        promo.DiscountValue = dto.DiscountValue;
        promo.MinOrderAmount = dto.MinOrderAmount;
        promo.MaxUsage = dto.MaxUsage;
        promo.StartDate = dto.StartDate;
        promo.EndDate = dto.EndDate;
        promo.IsActive = dto.IsActive;

        await _context.SaveChangesAsync();
        return Ok(new { message = "Cập nhật thành công." });
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var promo = await _context.Promotions.FindAsync(id);
        if (promo == null) return NotFound();
        _context.Promotions.Remove(promo);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Đã xóa." });
    }
}

public class ApplyPromotionDto
{
    public string Code { get; set; } = string.Empty;
    public decimal OrderAmount { get; set; }
}