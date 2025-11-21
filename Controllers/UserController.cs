using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Microsoft.Identity.Client;
using System.Security.Claims;
using TaskManagement_02.Data;
using TaskManagement_02.Models;

namespace TaskManagement_02.Controllers
{
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly PasswordHasher<UserModel> _passwordHasher = new();

        public UserController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult SignUp()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> SignUp(UserModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                TempData["Error"] = "Form is invalid: " + string.Join(", ", errors);
                return View(model);
            }

            var existing = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == model.Email);

            if (existing != null)
            {
                ModelState.AddModelError("Email", "Email already exists.");
                return View(model);
            }

            // Ensure default role is User if not provided
            model.Role = model.Role;

            // Hash password before saving
            model.Password = _passwordHasher.HashPassword(model, model.Password);

            _context.Users.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Registration successful! Please sign in.";
            return RedirectToAction("SignIn");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult SignIn()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> SignIn(string email, string password)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user != null)
            {
                var verification = _passwordHasher.VerifyHashedPassword(user, user.Password, password);
                if (verification == PasswordVerificationResult.Success)
                {
                    // Create claims and sign in with cookie authentication
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                        new Claim(ClaimTypes.Name, user.Name),
                        new Claim(ClaimTypes.Email, user.Email),
                        new Claim(ClaimTypes.Role, user.Role.ToString()) 
                    };

                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var principal = new ClaimsPrincipal(identity);

                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
                        new AuthenticationProperties
                        {
                            IsPersistent = false,
                            ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30)
                        });

                    // Optionally keep session values if you rely on them elsewhere
                    HttpContext.Session.SetInt32("UserId", user.Id);
                    HttpContext.Session.SetString("UserName", user.Name);

                    return RedirectToAction("Index", "Tasks");
                }
            }

            ViewBag.Error = "Invalid email or password.";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            return RedirectToAction("SignIn");
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId==null) return RedirectToAction("SignIn");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId.Value);
            if(user == null)
            {
                return NotFound();
            }
            return View(user);
        }

        [HttpGet]
        public async Task<IActionResult> ChangePassword()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToAction("SignIn");
            }
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return RedirectToAction("SignIn");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id==userId.Value);
            if (user ==null) return NotFound();
            // verify current Password
            var verification = _passwordHasher.VerifyHashedPassword(user, user.Password, model.CurrentPassword);
            if (verification != PasswordVerificationResult.Success)
            {
                ModelState.AddModelError(nameof(model.CurrentPassword), "Current password is incorrect.");
                return View(model);
            }
            // Hash and update new password 
            user.Password = _passwordHasher.HashPassword(user, model.NewPassword);
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Password changed successfully";

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            return RedirectToAction("SignIn");
        }
    }
}
