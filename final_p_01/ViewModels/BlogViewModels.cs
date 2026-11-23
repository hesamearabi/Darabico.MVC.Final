using System;
using System.Collections.Generic;
using final_p_01.Domain.Entities;

namespace final_p_01.ViewModels
{
    public class PostCardViewModel
    {
        public int Id { get; set; }
        public string Slug { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? CoverImagePath { get; set; }
        public string? CoverImageAlt { get; set; }
        public string? MetaDescription { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? CategoryName { get; set; }
    }

    public class PostDetailsViewModel
    {
        public Post Post { get; set; } = default!;
        public List<PostCardViewModel> RelatedPosts { get; set; } = new List<PostCardViewModel>();
        public List<PostCardViewModel> ChildPosts { get; set; } = new List<PostCardViewModel>();
    }

    public class BlogListViewModel
    {
        public List<PostCardViewModel> Posts { get; set; } = new List<PostCardViewModel>();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
    }
}