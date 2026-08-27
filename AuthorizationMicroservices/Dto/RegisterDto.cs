using System.ComponentModel.DataAnnotations;

namespace AuthorizationMicroservices.Dto;

public record RegisterDto(
    [Required]  string Name,
    [Required][EmailAddress] string Email,
    [Required]  string Password);