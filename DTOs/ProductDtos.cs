namespace MaxVerse.API.DTOs;

public class ProductListItemDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? DiscountPrice { get; set; }
    public string BrandName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public bool InStock { get; set; }
}

public class ProductDetailDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public decimal? DiscountPrice { get; set; }
    public string BrandName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public List<ProductImageDto> Images { get; set; } = new();
    public List<VariantDto> Variants { get; set; } = new();
}

public class VariantDto
{
    public int VariantId { get; set; }
    public string Size { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string? SKU { get; set; }
    public decimal? VariantPrice { get; set; }
    public string? VariantImageUrl { get; set; }
    public int? SizeId { get; set; }
    public int? ColorId { get; set; }
}

public class ProductFilterDto
{
    public string? Keyword { get; set; }
    public int? BrandId { get; set; }
    public int? CategoryId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string? SortBy { get; set; } // "price_asc", "price_desc", "newest"
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
}

public class CreateProductDto
{
    public string ProductName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public decimal? DiscountPrice { get; set; }
    public int BrandId { get; set; }
    public int CategoryId { get; set; }
    public string? ImageUrl { get; set; }
    public List<VariantInputDto> Variants { get; set; } = new();
}

public class VariantInputDto
{
    public string Size { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string? SKU { get; set; }
    public decimal? VariantPrice { get; set; }
    public string? VariantImageUrl { get; set; }
    public int? SizeId { get; set; }
    public int? ColorId { get; set; }
}

public class PagedResultDto<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
public class SizeDto
{
    public int SizeId { get; set; }
    public string SizeValue { get; set; } = string.Empty;
}

public class ColorDto
{
    public int ColorId { get; set; }
    public string ColorName { get; set; } = string.Empty;
    public string? ColorHex { get; set; }
}
public class AddProductImageDto
{
    public string ImageUrl { get; set; } = string.Empty;
}

public class ProductImageDto
{
    public int ImageId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
}
