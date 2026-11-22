using Darabico.Domain.Enums;

namespace Darabico.Web.ViewModels.Public;

public class ProjectCardViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public int CourtCount { get; set; }
    public CourtType CourtType { get; set; }
    public string ThumbnailUrl { get; set; } = string.Empty;
    public int ExecutionYear { get; set; }
}

public class ProjectIndexViewModel
{
    public List<ProjectCardViewModel> Projects { get; set; } = [];
    public List<string> AvailableCities { get; set; } = [];
    public string? SelectedCity { get; set; }
    public CourtType? SelectedCourtType { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 6;
    public int TotalCount { get; set; }
    public bool HasMore => CurrentPage * PageSize < TotalCount;
}

public class ProjectDetailViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public int CourtCount { get; set; }
    public CourtType CourtType { get; set; }
    public int ExecutionYear { get; set; }
    public int ExecutionDurationMonths { get; set; }
    public string Description { get; set; } = string.Empty;
    public string FeaturedImageUrl { get; set; } = string.Empty;
    public string? BeforeImageUrl { get; set; }
    public string? AfterImageUrl { get; set; }
    public string? VideoUrl { get; set; }
    public string MetaTitle { get; set; } = string.Empty;
    public string MetaDescription { get; set; } = string.Empty;
    public string CanonicalUrl { get; set; } = string.Empty;
    public List<ProjectGalleryItemViewModel> GalleryImages { get; set; } = [];
}

public class ProjectGalleryItemViewModel
{
    public int Id { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string AltText { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }
}