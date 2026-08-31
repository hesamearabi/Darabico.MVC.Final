using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using final_p_01.Areas.Admin.Models;
using final_p_01.Domain.Entities;
using final_p_01.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace final_p_01.Areas.Admin.Controllers
{
    [Area("Admin")]
    // 🚀 جلوگیری قطعی از کش شدن صفحات حساس لاگین توسط مرورگر یا پروکسی‌ها
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public class AuthController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly PasswordHasher<AdminUser> _passwordHasher;

        public AuthController(ApplicationDbContext context)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<AdminUser>();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Posts", new { area = "Admin" });
            }
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _context.AdminUsers.FirstOrDefaultAsync(u => u.Email == model.Email);

                if (user != null)
                {
                    var passwordVerification = _passwordHasher.VerifyHashedPassword(user, user.Password, model.Password);

                    if (passwordVerification == PasswordVerificationResult.Success || passwordVerification == PasswordVerificationResult.SuccessRehashNeeded)
                    {
                        var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.Name, user.Email),
                            new Claim(ClaimTypes.Role, "Administrator")
                        };
                        var identity = new ClaimsIdentity(claims, "DarabicoAuth");
                        var principal = new ClaimsPrincipal(identity);

                        var authProperties = new AuthenticationProperties
                        {
                            IsPersistent = true,
                            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                        };

                        await HttpContext.SignInAsync("DarabicoAuth", principal, authProperties);
                        return RedirectToAction("Index", "Posts", new { area = "Admin" });
                    }
                }

                // 🚀 Tarpit Mechanism: تاخیر تصادفی بین 1 تا 2.5 ثانیه برای کُند کردن ربات‌های هکر
                // این وقفه فقط برای لاگین‌های ناموفق اعمال می‌شود تا پرفورمنس کاربران واقعی افت نکند
                await Task.Delay(new Random().Next(1000, 2500));

                ModelState.AddModelError(string.Empty, "ایمیل یا رمز عبور اشتباه است.");
            }
            return View(model);
        }

        // 🚀 ایمن‌سازی متد خروج از سیستم در برابر حملات CSRF
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("DarabicoAuth");
            return RedirectToAction("Login");
        }
    }
}