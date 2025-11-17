using Microsoft.AspNetCore.Mvc;
using TaskManagement_02.Data;
using TaskManagement_02.Models;
using Microsoft.EntityFrameworkCore;

namespace TaskManagement_02.Controllers
{
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UserController(ApplicationDbContext context)
        {
            _context = context;
        }

       
        [HttpGet]
        public IActionResult SignUp()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SignUp(UserModel model)
        {
            if (!ModelState.IsValid)
            {
                // Debug helper to see what's invalid
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

                _context.Users.Add(model);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Registration successful! Please sign in.";
                return RedirectToAction("SignIn");
         }
           
        

      
        [HttpGet]
        public IActionResult SignIn()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SignIn(string email, string password)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email && u.Password == password);

            if (user != null)
            {
                HttpContext.Session.SetInt32("UserId", user.Id);
                HttpContext.Session.SetString("UserName", user.Name);

                TempData["Welcome"] = $"Welcome, {user.Name}!";
                return RedirectToAction("Index", "Tasks");
            }

            ViewBag.Error = "Invalid email or password.";
            return View();
        }

      
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("SignIn");
        }
    }
}
