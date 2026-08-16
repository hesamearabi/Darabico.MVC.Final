using System.ComponentModel.DataAnnotations;

namespace final_p_01.Areas.Admin.Models
{
    public class LoginViewModel
    {
        // فیلد یوزرنیم برای فرم لاگینی که ساختیم
        public string? Username { get; set; }

        // فیلد ایمیل که کنترلر شما به آن نیاز دارد
        [Required(ErrorMessage = "لطفاً ایمیل یا نام کاربری خود را وارد کنید.")]
        public string Email { get; set; }

        // فیلد رمز عبور
        [Required(ErrorMessage = "لطفاً رمز عبور خود را وارد کنید.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        // فیلد مرا به خاطر بسپار (معمولاً کنترلرهای پیش‌فرض به این فیلد هم گیر می‌دهند)
        public bool RememberMe { get; set; }
    }
}