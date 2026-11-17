using System.ComponentModel.DataAnnotations;

namespace final_p_01.Areas.Admin.Models;

public class AdminSettingsViewModel
{
    [Required(ErrorMessage = "لطفاً نام نمایشی را وارد کنید.")]
    [MaxLength(100, ErrorMessage = "نام نمایشی نمی‌تواند بیشتر از ۱۰۰ کاراکتر باشد.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "لطفاً ایمیل را وارد کنید.")]
    [EmailAddress(ErrorMessage = "فرمت ایمیل وارد شده معتبر نیست.")]
    [MaxLength(150, ErrorMessage = "ایمیل نمی‌تواند بیشتر از ۱۵۰ کاراکتر باشد.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "لطفاً رمز عبور فعلی را وارد کنید.")]
    [DataType(DataType.Password)]
    public string CurrentPassword { get; set; } = string.Empty;

    [MinLength(8, ErrorMessage = "رمز عبور جدید باید حداقل ۸ کاراکتر باشد.")]
    [DataType(DataType.Password)]
    public string? NewPassword { get; set; }

    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "رمز عبور جدید و تکرار آن مطابقت ندارند.")]
    public string? ConfirmNewPassword { get; set; }
}