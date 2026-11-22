using System.ComponentModel.DataAnnotations;

namespace Darabico.Domain.Enums;

public enum CourtType : byte
{
    [Display(Name = "روباز")]
    Outdoor = 1,

    [Display(Name = "سرپوشیده")]
    Indoor = 2
}