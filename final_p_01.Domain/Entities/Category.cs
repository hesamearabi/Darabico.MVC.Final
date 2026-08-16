// final_p_01.Domain/Entities/Category.cs
namespace final_p_01.Domain.Entities
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; } // برای سئو آدرس‌ها (مثلاً darabico.com/blog/category/net)

        // رابطه یک به چند: هر دسته بندی می‌تواند چندین پست داشته باشد
        public ICollection<Post> Posts { get; set; } = new List<Post>();
    }
}