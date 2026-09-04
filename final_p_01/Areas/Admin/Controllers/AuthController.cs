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
            // هدایت کاربر لاگین شده به پنل
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
            if (!ModelState.IsValid) return View(model);

            // 1. پیدا کردن کاربر بر اساس ایمیل وارد شده
            var user = await _context.AdminUsers.FirstOrDefaultAsync(u => u.Email == model.Email);

            if (user != null && !string.IsNullOrEmpty(user.Password))
            {
                bool isPasswordValid = false;
                bool needsUpgrade = false;

                try
                {
                    // 2. تلاش برای اعتبارسنجی هش استاندارد امنیتی
                    var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.Password, model.Password);

                    if (verificationResult == PasswordVerificationResult.Success)
                    {
                        isPasswordValid = true;
                    }
                    else if (verificationResult == PasswordVerificationResult.SuccessRehashNeeded)
                    {
                        isPasswordValid = true;
                        needsUpgrade = true;
                    }
                }
                catch (Exception)
                {
                    // 3. 🚀 رفع باگ Base-64: اگر پسورد در دیتابیس Plain-Text باشد، سیستم کرش نمی‌کند
                    if (user.Password == model.Password)
                    {
                        isPasswordValid = true;
                        needsUpgrade = true; // علامت‌گذاری برای ارتقا به هش امن
                    }
                }

                if (isPasswordValid)
                {
                    // 4. 🚀 عملیات ارتقای پسورد (Migration) به صورت خودکار
                    if (needsUpgrade)
                    {
                        user.Password = _passwordHasher.HashPassword(user, model.Password);
                        _context.AdminUsers.Update(user);
                        await _context.SaveChangesAsync();
                    }

                    // 5. ورود موفق و صدور کوکی امن
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

            // 🚀 مکانیزم Tarpit: ایجاد تاخیر تصادفی (۱ تا ۲ ثانیه) فقط برای ورودهای ناموفق جهت خنثی کردن هکرها
            await Task.Delay(Random.Shared.Next(1000, 2000));

            ModelState.AddModelError(string.Empty, "ایمیل یا رمز عبور اشتباه است.");
            return View(model);
        }

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