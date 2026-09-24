// Services/IProductService.cs

using FluentResults;
using MenuServices.DTOs;
using MenuServices.Models;
using MenuServices.Repository;

namespace MenuServices.Services;

public class ProductService(UnitOfWorkEfCore unitOfWork)
{
    private readonly UnitOfWorkEfCore _unitOfWork = unitOfWork;

    public async Task<Result<PaginatedResult<ProductDto>>> GetMenuAsync(MenuQueryParams queryParams)
    {
        if (queryParams.Page < 1)
            return Result.Fail<PaginatedResult<ProductDto>>(
                new ValidationError("Page must be greater than 0"));

        if (queryParams.PageSize < 1)
            return Result.Fail<PaginatedResult<ProductDto>>(
                new ValidationError("PageSize must be greater than 0"));

        var products = await _unitOfWork.productRepository.GetMenuAsync(queryParams);
        var totalCount = await _unitOfWork.productRepository.GetTotalCountAsync(queryParams);

        var result = new PaginatedResult<ProductDto>
        {
            Items = products.Select(MapToDto),
            TotalCount = totalCount,
            Page = queryParams.Page,
            PageSize = queryParams.PageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)queryParams.PageSize)
        };

        return Result.Ok(result);
    }

    public async Task<Result<ProductDto>> GetByIdAsync(int id)
    {
        var product = await _unitOfWork.productRepository.GetByIdAsync(id);
        if (product == null)
            return Result.Fail<ProductDto>(new NotFoundError($"Product {id} not found"));

        return Result.Ok(MapToDto(product));
    }

    public async Task<Result<ProductDto>> CreateAsync(CreateProductDto dto)
    {
        var validationErrors = ValidateCreate(dto);
        if (validationErrors.Any())
            return Result.Fail<ProductDto>(validationErrors);

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

        return Result.Ok(MapToDto(product));
    }

    public async Task<Result<ProductDto>> UpdateAsync(int id, UpdateProductDto dto)
    {
        var product = await _unitOfWork.productRepository.GetByIdAsync(id);
        if (product == null)
            return Result.Fail<ProductDto>(new NotFoundError($"Product {id} not found"));

        var validationErrors = ValidateUpdate(dto);
        if (validationErrors.Any())
            return Result.Fail<ProductDto>(validationErrors);

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

        return Result.Ok(MapToDto(product));
    }

    public async Task<Result> DeleteAsync(int id)
    {
        var product = await _unitOfWork.productRepository.GetByIdAsync(id);
        if (product == null)
            return Result.Fail(new NotFoundError($"Product {id} not found"));

        await _unitOfWork.productRepository.DeleteAsync(id);
        await _unitOfWork.SaveChangesAsync();

        return Result.Ok();
    }

    public async Task<Result> ToggleStopAsync(int id, bool isStopped)
    {
        var product = await _unitOfWork.productRepository.GetByIdAsync(id);
        if (product == null)
            return Result.Fail(new NotFoundError($"Product {id} not found"));

        if (product.IsStopped == isStopped)
            return Result.Ok(); // Идемпотентно — ничего не меняем

        product.IsStopped = isStopped;
        product.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.productRepository.UpdateAsync(product);
        await _unitOfWork.SaveChangesAsync();

        return Result.Ok();
    }

    // ---------- Валидация ----------

    private static List<IError> ValidateCreate(CreateProductDto dto)
    {
        var errors = new List<IError>();

        if (string.IsNullOrWhiteSpace(dto.Name))
            errors.Add(new ValidationError("Name is required"));

        if (dto.Price <= 0)
            errors.Add(new ValidationError("Price must be greater than 0"));

        if (string.IsNullOrWhiteSpace(dto.Category))
            errors.Add(new ValidationError("Category is required"));

        return errors;
    }

    private static List<IError> ValidateUpdate(UpdateProductDto dto)
    {
        var errors = new List<IError>();

        if (dto.Name != null && string.IsNullOrWhiteSpace(dto.Name))
            errors.Add(new ValidationError("Name cannot be empty"));

        if (dto.Price.HasValue && dto.Price.Value <= 0)
            errors.Add(new ValidationError("Price must be greater than 0"));

        if (dto.Category != null && string.IsNullOrWhiteSpace(dto.Category))
            errors.Add(new ValidationError("Category cannot be empty"));

        return errors;
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