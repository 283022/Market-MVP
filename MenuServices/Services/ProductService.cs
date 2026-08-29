// Services/IProductService.cs

using MenuServices.DTOs;
using MenuServices.Models;
using MenuServices.Repository;

namespace MenuServices.Services;

public class ProductService(UnitOfWorkEfCore unitOfWork)
{
    private readonly UnitOfWorkEfCore _unitOfWork = unitOfWork;

    public async Task<PaginatedResult<ProductDto>> GetMenuAsync(MenuQueryParams queryParams)
    {
        var products = await _unitOfWork.productRepository.GetMenuAsync(queryParams);
        var totalCount = await _unitOfWork.productRepository.GetTotalCountAsync(queryParams);

        
        return new PaginatedResult<ProductDto>
        {
            Items = products.Select(MapToDto),
            TotalCount = totalCount,
            Page = queryParams.Page,
            PageSize = queryParams.PageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)queryParams.PageSize)
        };
    }

    public async Task<ProductDto> GetByIdAsync(int id)
    {
        var product = await _unitOfWork.productRepository.GetByIdAsync(id);
        if (product == null)
            throw new KeyNotFoundException($"Product {id} not found");

        return MapToDto(product);
    }

    public async Task<ProductDto> CreateAsync(CreateProductDto dto)
    {
        var product = new Product
        {
            Name = dto.Name,
            Description = dto.Description,
            Price = dto.Price,
            ImageUrl = dto.ImageUrl,
            Category = dto.Category,
            IsActive = true,
            IsStopped = false,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.productRepository.AddAsync(product);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(product);
    }

    public async Task<ProductDto> UpdateAsync(int id, UpdateProductDto dto)
    {
        var product = await _unitOfWork.productRepository.GetByIdAsync(id);
        if (product == null)
            throw new KeyNotFoundException($"Product {id} not found");

        if (dto.Name != null) product.Name = dto.Name;
        if (dto.Description != null) product.Description = dto.Description;
        if (dto.Price.HasValue) product.Price = dto.Price.Value;
        if (dto.ImageUrl != null) product.ImageUrl = dto.ImageUrl;
        if (dto.Category != null) product.Category = dto.Category;
        if (dto.IsActive.HasValue) product.IsActive = dto.IsActive.Value;
        if (dto.IsStopped.HasValue) product.IsStopped = dto.IsStopped.Value;
        
        product.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.productRepository.UpdateAsync(product);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(product);
    }

    public async Task DeleteAsync(int id)
    {
        var product = await _unitOfWork.productRepository.GetByIdAsync(id);
        if (product == null)
            throw new KeyNotFoundException($"Product {id} not found");

        await _unitOfWork.productRepository.DeleteAsync(id);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task ToggleStopAsync(int id, bool isStopped)
    {
        var product = await _unitOfWork.productRepository.GetByIdAsync(id);
        if (product == null)
            throw new KeyNotFoundException($"Product {id} not found");

        product.IsStopped = isStopped;
        product.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.productRepository.UpdateAsync(product);
        await _unitOfWork.SaveChangesAsync();
    }

    private static ProductDto MapToDto(Product p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        Description = p.Description,
        Price = p.Price,
        ImageUrl = p.ImageUrl,
        Category = p.Category,
        IsStopped = p.IsStopped
    };
}