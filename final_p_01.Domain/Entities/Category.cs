using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace final_p_01.Domain.Entities
{
    public class Category
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; }

        [Required, MaxLength(100)]
        public string Slug { get; set; }

        public ICollection<Post> Posts { get; set; } = new List<Post>();
    }
}