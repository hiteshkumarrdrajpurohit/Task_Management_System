using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TaskManagement_02.Data;
using TaskManagement_02.Models;
using TaskManagement_02.Types;
using System.Linq;

namespace TaskManagement_02.Controllers
{
    public class TasksController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TasksController(ApplicationDbContext context)
        {
            _context = context;
        }

        // READ - List all tasks (include related entities)
        public async Task<IActionResult> Index()
        {
            var tasks = await _context.Tasks
                .Include(t => t.AssignedPerson)
                .Include(t => t.Category)
                .ToListAsync();

            return View(tasks);
        }

        // FILTER - List tasks by status (called by Index buttons)
        [HttpGet]
        public async Task<IActionResult> Filter(string status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return RedirectToAction(nameof(Index));

            if (!Enum.TryParse<Status>(status, true, out var parsedStatus))
                return RedirectToAction(nameof(Index));

            var tasks = await _context.Tasks
                .Include(t => t.AssignedPerson)
                .Include(t => t.Category)
                .Where(t => t.TaskStatus == parsedStatus)
                .ToListAsync();

            ViewData["Filter"] = parsedStatus.ToString();
            return View("Index", tasks);
        }

        // CREATE - GET
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadDropDowns();
            return View();
        }

        // CREATE - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TaskModel task)
        {
            if (ModelState.IsValid)
            {
                _context.Tasks.Add(task);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            await LoadDropDowns(task);
            return View(task);
        }

        // EDIT - GET
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var task = await _context.Tasks.FindAsync(id.Value);
            if (task == null) return NotFound();

            await LoadDropDowns(task);
            return View(task);
        }

        // EDIT - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TaskModel task)
        {
            if (id != task.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(task);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Tasks.AnyAsync(t => t.Id == id))
                        return NotFound();
                    throw;
                }

                return RedirectToAction(nameof(Index));
            }

            await LoadDropDowns(task);
            return View(task);
        }

        // DETAILS - GET
        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var task = await _context.Tasks
                .Include(t => t.AssignedPerson)
                .Include(t => t.Category)
                .FirstOrDefaultAsync(t => t.Id == id.Value);

            if (task == null) return NotFound();
            return View(task);
        }

        // DELETE - GET (confirmation)
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var task = await _context.Tasks
                .Include(t => t.AssignedPerson)
                .Include(t => t.Category)
                .FirstOrDefaultAsync(t => t.Id == id.Value);

            if (task == null) return NotFound();
            return View(task);
        }

        // DELETE - POST (existing)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var task = await _context.Tasks.FindAsync(id);
            if (task != null)
            {
                _context.Tasks.Remove(task);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        //  Helper method to avoid duplicate ViewBag code (async)
        private async Task LoadDropDowns(TaskModel? task = null)
        {
            var users = await _context.Users
                .OrderBy(u => u.Name)
                .ToListAsync();

            var categories = await _context.Categories
                .OrderBy(c => c.Name)
                .ToListAsync();

            ViewBag.Users = new SelectList(users, "Id", "Name", task?.AssignedPersonId);

            // Use CategoryModel.Name as the value/text and the TaskModel.CategoryName as selected value
            ViewBag.Categories = new SelectList(categories, "Name", "Name", task?.CategoryName);

            // Build Status list with explicit value/text so selected binding is deterministic
            ViewBag.StatusList = new SelectList(
                Enum.GetValues(typeof(Status))
                    .Cast<Status>()
                    .Select(s => new { Value = (int)s, Text = s.ToString() }),
                "Value",
                "Text",
                task is not null ? (int?)task.TaskStatus : null
            );
        }
    }
}