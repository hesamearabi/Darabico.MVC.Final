// Areas/Admin/Controllers/DashboardController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace final_p_01.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize] // این تگ باعث می‌شود فقط کاربران لاگین شده این صفحه را ببینند
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            return Content("خوش آمدید! شما با موفقیت وارد شدید.");
        }
    }
}