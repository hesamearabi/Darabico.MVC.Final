using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using final_p_01.Areas.Admin.Models;
using final_p_01.Domain.Entities;
using final_p_01.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace final_p_01.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
[ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
public class SettingsController : AdminBaseController
{
    private readonly ApplicationDbContext _context;
    private readonly PasswordHasher<AdminUser> _passwordHasher;

    public SettingsController(ApplicationDbContext context)
    {
        _context = context;
        _passwordHasher = new PasswordHasher<AdminUser>();
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var email = User.Identity?.Name;
        if (string.IsNullOrEmpty(email))
        {
            return RedirectToAction("Login", "Auth", new { area = "Admin" });
        }

        var admin = await _context.AdminUsers.FirstOrDefaultAsync(a => a.Email == email);
        if (admin == null)
        {
            return RedirectToAction("Login", "Auth", new { area = "Admin" });
        }

        var model = new AdminSettingsViewModel
        {
            FullName = admin.FullName,
            Email = admin.Email
        };

        var settings = await _context.SiteSettings.FirstOrDefaultAsync(s => s.Id == 1);
        ViewBag.SiteSeo = new SiteSeoViewModel
        {
            SeoHomeTitle = settings?.SeoHomeTitle,
            SeoHomeDescription = settings?.SeoHomeDescription,
            SeoProjectsTitle = settings?.SeoProjectsTitle,
            SeoProjectsDescription = settings?.SeoProjectsDescription,
            SeoBlogTitle = settings?.SeoBlogTitle,
            SeoBlogDescription = settings?.SeoBlogDescription,
            SeoEstimatorTitle = settings?.SeoEstimatorTitle,
            SeoEstimatorDescription = settings?.SeoEstimatorDescription,
            SeoInquiryTitle = settings?.SeoInquiryTitle,
            SeoInquiryDescription = settings?.SeoInquiryDescription,
            SeoGuideTitle = settings?.SeoGuideTitle,
            SeoGuideDescription = settings?.SeoGuideDescription,
            SeoAboutTitle = settings?.SeoAboutTitle,
            SeoAboutDescription = settings?.SeoAboutDescription,
            SeoContactTitle = settings?.SeoContactTitle,
            SeoContactDescription = settings?.SeoContactDescription,
            CurrentFaviconPath = settings?.FaviconPath
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(AdminSettingsViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ClearPasswordFields(model);
            return View(model);
        }

        var currentEmail = User.Identity?.Name;
        if (string.IsNullOrEmpty(currentEmail))
        {
            return RedirectToAction("Login", "Auth", new { area = "Admin" });
        }

        var admin = await _context.AdminUsers.FirstOrDefaultAsync(a => a.Email == currentEmail);
        if (admin == null)
        {
            return RedirectToAction("Login", "Auth", new { area = "Admin" });
        }

        bool isCurrentPasswordValid = false;
        bool needsUpgrade = false;

        if (!string.IsNullOrEmpty(admin.Password))
        {
            try
            {
                var verificationResult = _passwordHasher.VerifyHashedPassword(admin, admin.Password, model.CurrentPassword);
                if (verificationResult == PasswordVerificationResult.Success)
                {
                    isCurrentPasswordValid = true;
                }
                else if (verificationResult == PasswordVerificationResult.SuccessRehashNeeded)
                {
                    isCurrentPasswordValid = true;
                    needsUpgrade = true;
                }
            }
            catch (FormatException)
            {
                // Not a valid Identity hash: current password is treated as invalid.
            }
        }

        if (!isCurrentPasswordValid)
        {
            ModelState.AddModelError(nameof(model.CurrentPassword), "رمز عبور فعلی اشتباه است.");
            ClearPasswordFields(model);
            return View(model);
        }

        var normalizedEmail = model.Email.Trim();
        var normalizedFullName = model.FullName.Trim();
        bool emailChanged = !string.Equals(admin.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase);
        bool passwordChanged = !string.IsNullOrEmpty(model.NewPassword) || needsUpgrade;

        if (emailChanged)
        {
            bool emailExists = await _context.AdminUsers.AnyAsync(a => a.Id != admin.Id && a.Email == normalizedEmail);
            if (emailExists)
            {
                ModelState.AddModelError(nameof(model.Email), "این ایمیل قبلاً ثبت شده است.");
                ClearPasswordFields(model);
                return View(model);
            }
        }

        admin.Email = normalizedEmail;
        admin.FullName = normalizedFullName;

        if (!string.IsNullOrEmpty(model.NewPassword))
        {
            admin.Password = _passwordHasher.HashPassword(admin, model.NewPassword);
        }
        else if (needsUpgrade)
        {
            admin.Password = _passwordHasher.HashPassword(admin, model.CurrentPassword);
        }

        await _context.SaveChangesAsync();
        ViewBag.SuccessMessage = "اطلاعات حساب با موفقیت بروزرسانی شد!";

        if (emailChanged || passwordChanged)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, admin.Email),
                new Claim(ClaimTypes.Role, "Administrator"),
                new Claim(AuthController.PasswordFingerprintClaimType, AuthController.ComputePasswordFingerprint(admin.Password))
            };
            var identity = new ClaimsIdentity(claims, "DarabicoAuth");
            var principal = new ClaimsPrincipal(identity);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                AllowRefresh = true
            };
            await HttpContext.SignInAsync("DarabicoAuth", principal, authProperties);
        }

        ClearPasswordFields(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(2_000_000)]
    public async Task<IActionResult> SaveSeo(SiteSeoViewModel model, IFormFile? favicon)
    {
        var settings = await _context.SiteSettings.FirstOrDefaultAsync(s => s.Id == 1);
        if (settings == null)
        {
            settings = new SiteSettings { Id = Domain.Entities.SiteSettings.SingletonId };
            _context.SiteSettings.Add(settings);
        }

        settings.SeoHomeTitle = model.SeoHomeTitle?.Trim();
        settings.SeoHomeDescription = model.SeoHomeDescription?.Trim();
        settings.SeoProjectsTitle = model.SeoProjectsTitle?.Trim();
        settings.SeoProjectsDescription = model.SeoProjectsDescription?.Trim();
        settings.SeoBlogTitle = model.SeoBlogTitle?.Trim();
        settings.SeoBlogDescription = model.SeoBlogDescription?.Trim();
        settings.SeoEstimatorTitle = model.SeoEstimatorTitle?.Trim();
        settings.SeoEstimatorDescription = model.SeoEstimatorDescription?.Trim();
        settings.SeoInquiryTitle = model.SeoInquiryTitle?.Trim();
        settings.SeoInquiryDescription = model.SeoInquiryDescription?.Trim();
        settings.SeoGuideTitle = model.SeoGuideTitle?.Trim();
        settings.SeoGuideDescription = model.SeoGuideDescription?.Trim();
        settings.SeoAboutTitle = model.SeoAboutTitle?.Trim();
        settings.SeoAboutDescription = model.SeoAboutDescription?.Trim();
        settings.SeoContactTitle = model.SeoContactTitle?.Trim();
        settings.SeoContactDescription = model.SeoContactDescription?.Trim();
        settings.UpdatedAtUtc = DateTime.UtcNow;

        if (favicon != null && favicon.Length > 0)
        {
            var permitted = new[] { ".ico", ".png", ".svg" };
            var ext = Path.GetExtension(favicon.FileName).ToLowerInvariant();
            if (!permitted.Contains(ext) || favicon.Length > 512_000)
            {
                TempData["SeoError"] = "فاوآیکون باید فایل .ico یا .png یا .svg با حداکثر حجم ۵۰۰ کیلوبایت باشد.";
                return RedirectToAction(nameof(Index));
            }

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "branding");
            Directory.CreateDirectory(uploadsFolder);
            var fileName = $"favicon{ext}";
            var filePath = Path.Combine(uploadsFolder, fileName);
            await using (var stream = System.IO.File.Create(filePath))
            {
                await favicon.CopyToAsync(stream);
            }
            settings.FaviconPath = "/uploads/branding/favicon" + ext;
        }

        await _context.SaveChangesAsync();
        TempData["SeoSuccess"] = "تنظیمات سئو و فاوآیکون با موفقیت ذخیره شد.";
        return RedirectToAction(nameof(Index));
    }

    private void ClearPasswordFields(AdminSettingsViewModel model)
    {
        model.CurrentPassword = string.Empty;
        model.NewPassword = null;
        model.ConfirmNewPassword = null;

        ModelState.Remove(nameof(model.CurrentPassword));
        ModelState.Remove(nameof(model.NewPassword));
        ModelState.Remove(nameof(model.ConfirmNewPassword));
    }
}