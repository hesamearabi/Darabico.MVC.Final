using System.ComponentModel.DataAnnotations;
using Darabico.Domain.Entities;

namespace final_p_01.Domain.Entities
{
    public class Faq
    {
        public int Id { get; set; }

        [Required, MaxLength(500)]
        public string Question { get; set; } = string.Empty;

        [Required]
        public string Answer { get; set; } = string.Empty;

        public int? PostId { get; set; }
        public Post? Post { get; set; }

        public int? ProjectId { get; set; }
        public Project? Project { get; set; }

        public int? PillarPageId { get; set; }
        public PillarPage? PillarPage { get; set; }

        /// <summary>کلید صفحه برای نمایش آکاردئون در صفحات پیلار یا فرود (مثلا "pillar:slug")</summary>
        [MaxLength(100)]
        public string? PageKey { get; set; }

        public int SortOrder { get; set; }
    }
}