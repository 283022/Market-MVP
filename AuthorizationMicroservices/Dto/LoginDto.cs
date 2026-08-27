using System.ComponentModel.DataAnnotations;

namespace AuthorizationMicroservices.Dto;

public record LoginDto(
    [Required][EmailAddress]string Email,
    [Required] string Password);