using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManagement_02.Data;
using TaskManagement_02.Models;
using System.Threading.Tasks;

namespace TaskManagement_02.Controllers
{
    public class CategoryController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CategoryController(ApplicationDbContext context)
        {
            _context = context;
        }

        
        public async Task<IActionResult> Index()
        {
            var categories = await _context.Categories.ToListAsync();
            return View(categories);
        }

        public IActionResult Create()
        {
            return View();
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CategoryModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            _context.Categories.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Category added successfully!";
            return RedirectToAction("Index", "Tasks");
        }
    }
}
