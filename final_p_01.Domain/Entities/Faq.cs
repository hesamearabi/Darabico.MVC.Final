using System.ComponentModel.DataAnnotations;

namespace final_p_01.Domain.Entities
{
    public class Faq
    {
        public int Id { get; set; }

        [Required, MaxLength(500)]
        public string Question { get; set; }

        [Required]
        public string Answer { get; set; }

        public int PostId { get; set; }
        public Post? Post { get; set; }
    }
}