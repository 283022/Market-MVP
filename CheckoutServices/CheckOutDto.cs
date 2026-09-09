using System.ComponentModel.DataAnnotations;

namespace CheckoutService;

public record CheckOutDto(
    [Required] List<CheckOutItemDto> Items,
    string? Comment
);

public record CheckOutItemDto(
    [Required] Guid ProductId,
    [Required] int Quantity,
    [Required] bool IsSelected
);

