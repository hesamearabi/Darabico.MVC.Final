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
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace final_p_01.Areas.Admin.Controllers;

[Area("Admin")]
[ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
public class AuthController : Controller
{
    public const string PasswordFingerprintClaimType = "pwd_fp";

    private readonly ApplicationDbContext _context;
    private readonly PasswordHasher<AdminUser> _passwordHasher;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<AuthController> _logger;
    private readonly int _maxFailedAttempts;
    private readonly TimeSpan _lockoutDuration;

    public AuthController(
        ApplicationDbContext context,
        IMemoryCache memoryCache,
        ILogger<AuthController> logger,
        IConfiguration configuration)
    {
        _context = context;
        _memoryCache = memoryCache;
        _logger = logger;
        _passwordHasher = new PasswordHasher<AdminUser>();
        _maxFailedAttempts = configuration.GetValue<int?>("AdminAuth:MaxFailedAttempts") ?? 5;
        var lockoutMinutes = configuration.GetValue<double?>("AdminAuth:LockoutMinutes") ?? 15;
        _lockoutDuration = TimeSpan.FromMinutes(lockoutMinutes);
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
    [EnableRateLimiting("AdminLoginRateLimit")]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var normalizedEmail = model.Email.Trim();
        var accountKey = normalizedEmail.ToLowerInvariant();
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var safeEmailForLog = SanitizeForLog(normalizedEmail);

        // NOTE: Lockout counters are stored in local IMemoryCache per application instance.
        // If deployed across multiple servers or IIS worker processes (Web Garden), switch to IDistributedCache (Redis/SQL Server).
        if (IsAccountLockedOut(accountKey, out var lockoutEndUtc))
        {
            _logger.LogWarning(
                "Blocked admin login attempt for locked-out account {Email} from IP {ClientIp} until {LockoutEndUtc:O}.",
                safeEmailForLog,
                clientIp,
                lockoutEndUtc);

            await Task.Delay(Random.Shared.Next(1000, 2000));
            ModelState.AddModelError(string.Empty, "ایمیل یا رمز عبور اشتباه است.");
            return View(model);
        }

        var user = await _context.AdminUsers.FirstOrDefaultAsync(u => u.Email == normalizedEmail);

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
            catch (FormatException)
            {
                // Not a valid Identity hash: password is treated as invalid.
            }

            if (isPasswordValid)
            {
                ClearFailedAttempts(accountKey);

                if (needsUpgrade)
                {
                    user.Password = _passwordHasher.HashPassword(user, model.Password);
                    await _context.SaveChangesAsync();
                }

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Email),
                    new Claim(ClaimTypes.Role, "Administrator"),
                    new Claim(PasswordFingerprintClaimType, ComputePasswordFingerprint(user.Password))
                };

                var identity = new ClaimsIdentity(claims, "DarabicoAuth");
                var principal = new ClaimsPrincipal(identity);

                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    AllowRefresh = true
                };

                await HttpContext.SignInAsync("DarabicoAuth", principal, authProperties);

                _logger.LogInformation(
                    "Successful admin login for user {AdminId} ({Email}) from IP {ClientIp}.",
                    user.Id,
                    safeEmailForLog,
                    clientIp);

                return RedirectToAction("Index", "Posts", new { area = "Admin" });
            }
        }

        var attemptState = RecordFailedAttempt(accountKey);
        if (attemptState.LockoutEndUtc.HasValue)
        {
            _logger.LogWarning(
                "Failed admin login attempt #{FailedCount} for {Email} from IP {ClientIp}. Account temporarily locked until {LockoutEndUtc:O}.",
                attemptState.FailedCount,
                safeEmailForLog,
                clientIp,
                attemptState.LockoutEndUtc.Value);
        }
        else
        {
            _logger.LogWarning(
                "Failed admin login attempt #{FailedCount} for {Email} from IP {ClientIp}.",
                attemptState.FailedCount,
                safeEmailForLog,
                clientIp);
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
        return RedirectToAction("Login", "Auth", new { area = "Admin" });
    }

    public static string ComputePasswordFingerprint(string passwordHash)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(passwordHash));
        return Convert.ToHexString(hashBytes);
    }

    public static string SanitizeForLog(string value)
    {
        return value.Replace("\r", string.Empty).Replace("\n", string.Empty);
    }

    private static string GetLockoutCacheKey(string normalizedAccountKey) => $"admin_login_lockout:{normalizedAccountKey}";

    private bool IsAccountLockedOut(string normalizedAccountKey, out DateTimeOffset lockoutEndUtc)
    {
        lockoutEndUtc = default;
        if (!_memoryCache.TryGetValue(GetLockoutCacheKey(normalizedAccountKey), out LoginAttemptState? state) || state == null)
        {
            return false;
        }

        lock (state)
        {
            if (state.LockoutEndUtc.HasValue && state.LockoutEndUtc.Value > DateTimeOffset.UtcNow)
            {
                lockoutEndUtc = state.LockoutEndUtc.Value;
                return true;
            }
        }

        return false;
    }

    private LoginAttemptState RecordFailedAttempt(string normalizedAccountKey)
    {
        var cacheKey = GetLockoutCacheKey(normalizedAccountKey);
        var state = _memoryCache.GetOrCreate(cacheKey, entry =>
        {
            entry.SlidingExpiration = _lockoutDuration;
            return new LoginAttemptState();
        })!;

        lock (state)
        {
            if (state.LockoutEndUtc.HasValue && state.LockoutEndUtc.Value <= DateTimeOffset.UtcNow)
            {
                state.FailedCount = 0;
                state.LockoutEndUtc = null;
            }

            state.FailedCount++;
            if (state.FailedCount >= _maxFailedAttempts)
            {
                state.LockoutEndUtc = DateTimeOffset.UtcNow.Add(_lockoutDuration);
            }

            _memoryCache.Set(cacheKey, state, new MemoryCacheEntryOptions
            {
                SlidingExpiration = _lockoutDuration
            });

            return new LoginAttemptState
            {
                FailedCount = state.FailedCount,
                LockoutEndUtc = state.LockoutEndUtc
            };
        }
    }

    private void ClearFailedAttempts(string normalizedAccountKey)
    {
        _memoryCache.Remove(GetLockoutCacheKey(normalizedAccountKey));
    }

    private sealed class LoginAttemptState
    {
        public int FailedCount { get; set; }
        public DateTimeOffset? LockoutEndUtc { get; set; }
    }
}