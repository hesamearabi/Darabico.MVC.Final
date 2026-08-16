using System.ComponentModel.DataAnnotations;

namespace final_p_01.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "لطفاً نام کاربری یا ایمیل خود را وارد کنید.")]
        public string Username { get; set; }

        [Required(ErrorMessage = "لطفاً رمز عبور خود را وارد کنید.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}