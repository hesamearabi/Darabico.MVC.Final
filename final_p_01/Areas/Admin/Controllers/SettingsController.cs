using final_p_01.Domain.Entities;
using final_p_01.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;

namespace final_p_01.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class SettingsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SettingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var admin = _context.AdminUsers.FirstOrDefault();
            return View(admin);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(AdminUser model)
        {
            var admin = _context.AdminUsers.FirstOrDefault(a => a.Id == model.Id);
            if (admin != null)
            {
                admin.Email = model.Email;
                admin.FullName = model.FullName;

                // اگر رمز جدیدی وارد کرده بود، آن را آپدیت کن
                if (!string.IsNullOrEmpty(model.Password))
                {
                    admin.Password = model.Password;
                }

                _context.SaveChanges();
                ViewBag.SuccessMessage = "اطلاعات حساب با موفقیت بروزرسانی شد!";
            }
            return View(admin);
        }
    }
}