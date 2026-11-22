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

        public int? PostId { get; set; }
        public Post? Post { get; set; }

        /// <summary>کلید صفحه برای نمایش آکاردئون در صفحات پیلار یا فرود (مثلا "pillar:slug")</summary>
        [MaxLength(100)]
        public string? PageKey { get; set; }

        public int SortOrder { get; set; }
    }
}