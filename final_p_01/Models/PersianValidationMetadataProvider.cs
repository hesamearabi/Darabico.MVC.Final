using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace final_p_01.Models;

public sealed class PersianValidationMetadataProvider : IValidationMetadataProvider
{
    public void CreateValidationMetadata(ValidationMetadataProviderContext context)
    {
        foreach (var attribute in context.ValidationMetadata.ValidatorMetadata.OfType<ValidationAttribute>())
        {
            if (attribute.ErrorMessage != null || attribute.ErrorMessageResourceType != null) continue;
            attribute.ErrorMessage = attribute switch
            {
                RequiredAttribute => "تکمیل «{0}» الزامی است.",
                MaxLengthAttribute => "«{0}» نباید بیش از {1} کاراکتر باشد.",
                StringLengthAttribute => "«{0}» نباید بیش از {1} کاراکتر باشد.",
                MinLengthAttribute => "«{0}» باید حداقل {1} کاراکتر باشد.",
                RangeAttribute => "«{0}» باید بین {1} و {2} باشد.",
                EmailAddressAttribute => "نشانی ایمیل معتبر وارد کنید.",
                UrlAttribute => "نشانی اینترنتی معتبر وارد کنید.",
                _ => "مقدار «{0}» معتبر نیست."
            };
        }
    }
}
