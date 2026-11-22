using System;
using System.ComponentModel.DataAnnotations;

namespace final_p_01.Domain.Entities;

public class PostSlugHistory
{
    public int Id { get; set; }

    public int PostId { get; set; }
    public Post Post { get; set; } = null!;

    [Required, MaxLength(200)]
    public string Slug { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}