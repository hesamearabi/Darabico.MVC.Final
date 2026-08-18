using System.Collections.Generic;
using final_p_01.Domain.Entities;

namespace final_p_01.Models
{
    public class PostDetailsViewModel
    {
        public Post Post { get; set; } = default!;
        public List<Post> RelatedPosts { get; set; } = new List<Post>();
    }

    public class BlogListViewModel
    {
        public List<Post> Posts { get; set; } = new List<Post>();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
    }
}