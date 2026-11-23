using System.ComponentModel.DataAnnotations;

namespace final_p_01.Areas.Admin.Models;

public class SiteSeoViewModel
{
    [MaxLength(200)] public string? SeoHomeTitle { get; set; }
    [MaxLength(320)] public string? SeoHomeDescription { get; set; }
    [MaxLength(200)] public string? SeoProjectsTitle { get; set; }
    [MaxLength(320)] public string? SeoProjectsDescription { get; set; }
    [MaxLength(200)] public string? SeoBlogTitle { get; set; }
    [MaxLength(320)] public string? SeoBlogDescription { get; set; }
    [MaxLength(200)] public string? SeoEstimatorTitle { get; set; }
    [MaxLength(320)] public string? SeoEstimatorDescription { get; set; }
    [MaxLength(200)] public string? SeoInquiryTitle { get; set; }
    [MaxLength(320)] public string? SeoInquiryDescription { get; set; }
    [MaxLength(200)] public string? SeoGuideTitle { get; set; }
    [MaxLength(320)] public string? SeoGuideDescription { get; set; }
    [MaxLength(200)] public string? SeoAboutTitle { get; set; }
    [MaxLength(320)] public string? SeoAboutDescription { get; set; }
    [MaxLength(200)] public string? SeoContactTitle { get; set; }
    [MaxLength(320)] public string? SeoContactDescription { get; set; }
    public string? CurrentFaviconPath { get; set; }
}
