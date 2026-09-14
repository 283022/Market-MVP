using System.ComponentModel.DataAnnotations;

namespace CheckoutService.DTOs;

public record CheckOutDto(
    [Required] List<CheckOutItemDto> Items,
    string? Comment
);

public record CheckOutItemDto(
    [Required] Guid ProductId,
    [Required] int Quantity,
    [Required] bool IsSelected
);


public class CheckoutResult
{
    public Guid OrderId { get; set; }
    public string PaymentUrl { get; set; }
    public DateTime ExpiresAt { get; set; }
}