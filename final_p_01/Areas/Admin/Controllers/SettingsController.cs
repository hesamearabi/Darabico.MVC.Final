using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using final_p_01.Areas.Admin.Models;
using final_p_01.Domain.Entities;
using final_p_01.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace final_p_01.Areas.Admin.Controllers;

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
    public IActionResult Index()
    {
        var email = User.Identity?.Name;
        var admin = _context.AdminUsers.FirstOrDefault(a => a.Email == email);

        if (admin == null) return RedirectToAction("Login", "Auth");

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
        if (!ModelState.IsValid) return View(model);

        var currentEmail = User.Identity?.Name;
        var admin = _context.AdminUsers.FirstOrDefault(a => a.Email == currentEmail);

        if (admin != null)
        {
            bool isCurrentPasswordValid = false;

            try
            {
                var verificationResult = _passwordHasher.VerifyHashedPassword(admin, admin.Password, model.CurrentPassword);
                if (verificationResult == PasswordVerificationResult.Success || verificationResult == PasswordVerificationResult.SuccessRehashNeeded)
                {
                    isCurrentPasswordValid = true;
                }
            }
            catch (Exception)
            {
                // TODO: Delete this compatibility path once all legacy passwords are migrated to hashes
                if (CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(admin.Password), Encoding.UTF8.GetBytes(model.CurrentPassword)))
                {
                    isCurrentPasswordValid = true;
                }
            }

            if (!isCurrentPasswordValid)
            {
                ModelState.AddModelError("CurrentPassword", "رمز عبور فعلی اشتباه است.");
                return View(model);
            }

            bool emailChanged = admin.Email != model.Email;

            admin.Email = model.Email;
            admin.FullName = model.FullName;

            if (!string.IsNullOrEmpty(model.NewPassword))
            {
                admin.Password = _passwordHasher.HashPassword(admin, model.NewPassword);
            }
            else if (admin.Password == model.CurrentPassword)
            {
                admin.Password = _passwordHasher.HashPassword(admin, model.CurrentPassword);
            }

            await _context.SaveChangesAsync();
            ViewBag.SuccessMessage = "اطلاعات حساب با موفقیت بروزرسانی شد!";

            if (emailChanged)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, admin.Email),
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
            }

            model.CurrentPassword = string.Empty;
            model.NewPassword = string.Empty;
            model.ConfirmNewPassword = string.Empty;
        }

        return View(model);
    }
}