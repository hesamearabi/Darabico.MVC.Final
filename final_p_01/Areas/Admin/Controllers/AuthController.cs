using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
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

        public AuthController(ApplicationDbContext context)
        {
            _context = context;
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
                if (!_context.AdminUsers.Any())
                {
                    _context.AdminUsers.Add(new AdminUser { Email = "admin@darabico.com", Password = "123" });
                    _context.SaveChanges();
                }

                var user = _context.AdminUsers.FirstOrDefault(u => u.Email == model.Email && u.Password == model.Password);

                if (user != null)
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, user.Email)
                    };
                    var identity = new ClaimsIdentity(claims, "DarabicoAuth");
                    var principal = new ClaimsPrincipal(identity);

                    await HttpContext.SignInAsync("DarabicoAuth", principal);

                    return RedirectToAction("Index", "Posts", new { area = "Admin" });
                }

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