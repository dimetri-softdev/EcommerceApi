using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace StoreApi.Models;

public class AppUser : IdentityUser
{
    [Required]
    [StringLength(120)]
    public string FullName { get; set; } = string.Empty;
}
