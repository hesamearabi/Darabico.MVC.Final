using System.ComponentModel.DataAnnotations;

namespace final_p_01.Areas.Admin.Models;

public class AdminSettingsViewModel
{
    [Required(ErrorMessage = "نام نمایشی الزامی است.")]
    public string FullName { get; set; }

    [Required(ErrorMessage = "ایمیل الزامی است.")]
    [EmailAddress(ErrorMessage = "ایمیل نامعتبر است.")]
    public string Email { get; set; }

    [Required(ErrorMessage = "رمز عبور فعلی برای اعمال تغییرات الزامی است.")]
    [DataType(DataType.Password)]
    public string CurrentPassword { get; set; }

    [DataType(DataType.Password)]
    public string? NewPassword { get; set; }

    [DataType(DataType.Password)]
    [Compare("NewPassword", ErrorMessage = "رمز عبور جدید و تکرار آن یکسان نیستند.")]
    public string? ConfirmNewPassword { get; set; }
}