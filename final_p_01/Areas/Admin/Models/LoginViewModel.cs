using System.ComponentModel.DataAnnotations;

namespace final_p_01.Areas.Admin.Models;

public class LoginViewModel
{
    public string? Username { get; set; }

    [Required(ErrorMessage = "لطفاً ایمیل یا نام کاربری خود را وارد کنید.")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "لطفاً رمز عبور خود را وارد کنید.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = null!;

    public bool RememberMe { get; set; }
}