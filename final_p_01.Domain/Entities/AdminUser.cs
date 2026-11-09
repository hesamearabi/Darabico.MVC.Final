using System.ComponentModel.DataAnnotations;

namespace final_p_01.Domain.Entities;

public class AdminUser
{
    public int Id { get; set; }

    [Required, MaxLength(150)]
    public string Email { get; set; }

    [Required, MaxLength(500)]
    public string Password { get; set; }

    [MaxLength(100)]
    public string FullName { get; set; } = "مدیر دارابیکو";
}