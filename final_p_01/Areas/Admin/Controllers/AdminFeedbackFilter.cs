using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace final_p_01.Areas.Admin.Controllers;

public sealed class AdminFeedbackFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var result = await next();
        if (context.Controller is PhaseModulesController controller &&
            HttpMethods.IsPost(context.HttpContext.Request.Method) && result.Exception == null &&
            result.Result is RedirectToActionResult && !controller.TempData.ContainsKey("Error") &&
            !controller.TempData.ContainsKey("Success"))
            controller.TempData["Success"] = "تغییرات با موفقیت ذخیره شد.";
    }
}
