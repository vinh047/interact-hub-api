using System.ComponentModel.DataAnnotations;

namespace InteractHub.Api.DTOs;

public class RegisterRequest
{
    [Required]
    [EmailAddress]
    public required string Email { get; set; }

    [Required]
    [MinLength(6)]
    public required string Password { get; set; }

    [Required]
    public required string FullName { get; set; }
}