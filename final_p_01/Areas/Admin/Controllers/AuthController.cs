using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity; // برای PasswordHasher
using Microsoft.EntityFrameworkCore; // برای دستورات Async دیتابیس
using final_p_01.Areas.Admin.Models;
using final_p_01.Domain.Entities;
using final_p_01.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;

namespace final_p_01.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AuthController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly PasswordHasher<AdminUser> _passwordHasher;

        public AuthController(ApplicationDbContext context)
        {
            _context = context;
            // نمونه‌سازی ابزار هش کردن پسورد
            _passwordHasher = new PasswordHasher<AdminUser>();
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Posts", new { area = "Admin" });
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                // 1. بررسی و Seed کردن ادمین اولیه به صورت کاملاً امن
                if (!await _context.AdminUsers.AnyAsync())
                {
                    var newAdmin = new AdminUser
                    {
                        Email = "admin@darabico.com"
                    };

                    // رمز عبور هش شده و امن ذخیره می‌شود (رمز پیش‌فرض: Admin@123!)
                    newAdmin.Password = _passwordHasher.HashPassword(newAdmin, "Admin@123!");

                    _context.AdminUsers.Add(newAdmin);
                    await _context.SaveChangesAsync();
                }

                // 2. پیدا کردن کاربر فقط بر اساس ایمیل
                var user = await _context.AdminUsers.FirstOrDefaultAsync(u => u.Email == model.Email);

                if (user != null)
                {
                    // 3. مقایسه امن رمز عبور هش شده با رمز وارد شده
                    var passwordVerification = _passwordHasher.VerifyHashedPassword(user, user.Password, model.Password);

                    if (passwordVerification == PasswordVerificationResult.Success || passwordVerification == PasswordVerificationResult.SuccessRehashNeeded)
                    {
                        // 4. تولید Claim ها و لاگین در صورت صحت اطلاعات
                        var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.Name, user.Email),
                            new Claim(ClaimTypes.Role, "Administrator") // افزوده شدن نقش
                        };
                        var identity = new ClaimsIdentity(claims, "DarabicoAuth");
                        var principal = new ClaimsPrincipal(identity);

                        // 5. تنظیمات امنیتی کوکی
                        var authProperties = new AuthenticationProperties
                        {
                            IsPersistent = true, // حفظ لاگین بعد از بستن مرورگر
                            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                        };

                        await HttpContext.SignInAsync("DarabicoAuth", principal, authProperties);

                        return RedirectToAction("Index", "Posts", new { area = "Admin" });
                    }
                }

                // پیام خطای یکسان برای جلوگیری از لو رفتن وجود یا عدم وجود ایمیل در دیتابیس
                ModelState.AddModelError(string.Empty, "ایمیل یا رمز عبور اشتباه است.");
            }
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("DarabicoAuth");
            return RedirectToAction("Login");
        }
    }
}