// final_p_01.Domain/Entities/Post.cs
namespace final_p_01.Domain.Entities
{
    public class Post
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Slug { get; set; } // آدرس سئوی مقاله
        public string Content { get; set; } // محتوای متنی یا HTML مقاله
        public string? MetaDescription { get; set; } // توضیحات سئو برای گوگل
        public bool IsPublished { get; set; } // وضعیت انتشار
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // رابطه با جدول دسته‌بندی
        public int CategoryId { get; set; }
        public Category Category { get; set; } = null!;
        public string? CoverImagePath { get; set; }
        // داخل کلاس Post این خط را اضافه کنید
        public ICollection<Faq> Faqs { get; set; }
    }
}