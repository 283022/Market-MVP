// DTOs/ProductDto.cs
namespace MenuServices.DTOs;

public record ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public decimal Price { get; set; }
    public string ImageUrl { get; set; }
    public string Category { get; set; }
    public bool IsStopped { get; set; }
}

// DTOs/CreateProductDto.cs
public class CreateProductDto
{
    public string Name { get; set; }
    public string Description { get; set; }
    public decimal Price { get; set; }
    public string ImageUrl { get; set; }
    public string Category { get; set; }
}

// DTOs/UpdateProductDto.cs
public class UpdateProductDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public decimal? Price { get; set; }
    public string? ImageUrl { get; set; }
    public string? Category { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsStopped { get; set; }
    public int? SortOrder { get; set; }
}

// DTOs/MenuQueryParams.cs
public class MenuQueryParams
{
    public string? Filter { get; set; }
    public string? Category { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class PaginatedResult<T>
{
    public IEnumerable<T> Items { get; set; }
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

public class StopProductDto
{
    public bool IsStopped { get; set; }
}