using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using final_p_01.Areas.Admin.Models;
using final_p_01.Domain.Entities;
using final_p_01.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace final_p_01.Areas.Admin.Controllers;

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

        var user = await _context.AdminUsers.FirstOrDefaultAsync(u => u.Email == model.Email);

        if (user != null && !string.IsNullOrEmpty(user.Password))
        {
            bool isPasswordValid = false;
            bool needsUpgrade = false;

            try
            {
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
                // TODO: Delete this compatibility path once all legacy passwords are migrated to hashes
                if (CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(user.Password), Encoding.UTF8.GetBytes(model.Password)))
                {
                    isPasswordValid = true;
                    needsUpgrade = true;
                }
            }

            if (isPasswordValid)
            {
                if (needsUpgrade)
                {
                    user.Password = _passwordHasher.HashPassword(user, model.Password);
                    _context.AdminUsers.Update(user);
                    await _context.SaveChangesAsync();
                }

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