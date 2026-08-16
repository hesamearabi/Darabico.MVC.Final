using System.ComponentModel.DataAnnotations;

namespace final_p_01.Domain.Entities
{
    public class AdminUser
    {
        public int Id { get; set; }

        [Required]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }

        public string FullName { get; set; } = "مدیر دارابیکو";
    }
}