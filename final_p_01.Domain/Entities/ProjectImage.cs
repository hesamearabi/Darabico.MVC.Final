using System.ComponentModel.DataAnnotations;

namespace Darabico.Domain.Entities;

public class ProjectImage
{
    public int Id { get; set; }

    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    [Required]
    [MaxLength(500)]
    public string ImageUrl { get; set; } = string.Empty;

    [Required(ErrorMessage = "متن جایگزین (Alt) برای سئو الزامی است.")]
    [MaxLength(250)]
    public string AltText { get; set; } = string.Empty;

    public int Width { get; set; }

    public int Height { get; set; }

    public int SortOrder { get; set; }
}