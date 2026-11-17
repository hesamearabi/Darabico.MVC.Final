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
                // Handled below by legacy plaintext compatibility check
            }

            // TODO: Delete this legacy plaintext compatibility path once all existing AdminUser rows are migrated to Identity v3 hashes.
            if (!isCurrentPasswordValid && !IsIdentityPasswordHash(admin.Password))
            {
                if (CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(admin.Password),
                    Encoding.UTF8.GetBytes(model.CurrentPassword)))
                {
                    isCurrentPasswordValid = true;
                    needsUpgrade = true;
                }
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

    private void ClearPasswordFields(AdminSettingsViewModel model)
    {
        model.CurrentPassword = string.Empty;
        model.NewPassword = null;
        model.ConfirmNewPassword = null;

        ModelState.Remove(nameof(model.CurrentPassword));
        ModelState.Remove(nameof(model.NewPassword));
        ModelState.Remove(nameof(model.ConfirmNewPassword));
    }

    // TODO: Delete this helper together with the legacy plaintext compatibility path once all rows are migrated.
    private static bool IsIdentityPasswordHash(string value)
    {
        if (string.IsNullOrEmpty(value)) return false;
        try
        {
            var decoded = Convert.FromBase64String(value);
            return decoded.Length >= 49 && (decoded[0] == 0x00 || decoded[0] == 0x01);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}