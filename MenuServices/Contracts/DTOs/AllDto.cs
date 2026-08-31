namespace MenuServices.Contracts.DTOs;

public record ProductDto(
    Guid Id,
    string Name,
    decimal Price,
    string ImageUrl,
    bool IsStopped,
    string Category
);