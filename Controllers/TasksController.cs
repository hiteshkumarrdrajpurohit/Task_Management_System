using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TaskManagement_02.Data;
using TaskManagement_02.Models;
using TaskManagement_02.Types;
using System.Linq;

namespace TaskManagement_02.Controllers
{
    [Authorize]
    public class TasksController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TasksController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int? GetCurrentUserId()
        {
            var id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(id, out var i) ? i : (int?)null;
        }

        private bool IsAdmin() => User.IsInRole("Admin");

        // READ - List tasks; admin sees all, users see only their tasks
        public async Task<IActionResult> Index()
        {
            var query = _context.Tasks
                .Include(t => t.AssignedPerson)
                .Include(t => t.Category)
                .AsQueryable();

            if (!IsAdmin())
            {
                var userId = GetCurrentUserId();
                if (userId == null) return Forbid();
                query = query.Where(t => t.AssignedPersonId == userId.Value);
            }

            var tasks = await query.ToListAsync();
            return View(tasks);
        }

        // FILTER - List tasks by status
        public async Task<IActionResult> Filter(string status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return RedirectToAction(nameof(Index));

            if (!Enum.TryParse<Status>(status, true, out var parsedStatus))
                return RedirectToAction(nameof(Index));

            var query = _context.Tasks
                .Include(t => t.AssignedPerson)
                .Include(t => t.Category)
                .Where(t => t.TaskStatus == parsedStatus)
                .AsQueryable();

            if (!IsAdmin())
            {
                var userId = GetCurrentUserId();
                if (userId == null) return Forbid();
                query = query.Where(t => t.AssignedPersonId == userId.Value);
            }

            var tasks = await query.ToListAsync();
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

        // CREATE - POST (server-side validation for dates and assigned user)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TaskModel task)
        {
            // Validate assigned person selection exists
            if (task.AssignedPersonId <= 0)
            {
                ModelState.AddModelError(nameof(task.AssignedPersonId), "Please select a user to assign.");
            }
            else
            {
                var assignedUser = await _context.Users.FindAsync(task.AssignedPersonId);
                if (assignedUser == null)
                {
                    ModelState.AddModelError(nameof(task.AssignedPersonId), "Selected user not found.");
                }
                else if (!IsAdmin() && assignedUser.Role == RoleType.Admin)
                {
                    // Non-admins must not assign to Admins
                    ModelState.AddModelError(nameof(task.AssignedPersonId), "You cannot assign tasks to Admin users.");
                }
            }

            // Server-side date validation: AssignedDate must be on-or-before SubmissionDate
            if (task.AssignedDate != default && task.SubmissionDate != default && task.AssignedDate > task.SubmissionDate)
            {
                ModelState.AddModelError(nameof(task.AssignedDate), "Assigned Date must be on or before Submission Date.");
            }

            if (!ModelState.IsValid)
            {
                await LoadDropDowns(task);
                return View(task);
            }

            // All validations passed — create the task
            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // EDIT - GET
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var task = await _context.Tasks.FindAsync(id.Value);
            if (task == null) return NotFound();

            // authorization: only owner or admin
            if (!IsAdmin())
            {
                var userId = GetCurrentUserId();
                if (userId == null || task.AssignedPersonId != userId.Value)
                    return Forbid();
            }

            await LoadDropDowns(task);
            return View(task);
        }

        // EDIT - POST (server-side date validation and secure ownership check)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TaskModel task)
        {
            if (id != task.Id) return NotFound();

            // Load original task to verify ownership and prevent tampering
            var dbTask = await _context.Tasks.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
            if (dbTask == null) return NotFound();

            // authorization: only original owner or admin can edit
            if (!IsAdmin())
            {
                var userId = GetCurrentUserId();
                if (userId == null || dbTask.AssignedPersonId != userId.Value)
                    return Forbid();
            }

            // Validate assigned person selection exists and role constraints
            if (task.AssignedPersonId <= 0)
            {
                ModelState.AddModelError(nameof(task.AssignedPersonId), "Please select a user to assign.");
            }
            else
            {
                var assignedUser = await _context.Users.FindAsync(task.AssignedPersonId);
                if (assignedUser == null)
                {
                    ModelState.AddModelError(nameof(task.AssignedPersonId), "Selected user not found.");
                }
                else if (!IsAdmin() && assignedUser.Role == RoleType.Admin)
                {
                    ModelState.AddModelError(nameof(task.AssignedPersonId), "You cannot assign tasks to Admin users.");
                }
            }

            // Server-side date validation
            if (task.AssignedDate != default && task.SubmissionDate != default && task.AssignedDate > task.SubmissionDate)
            {
                ModelState.AddModelError(nameof(task.AssignedDate), "Assigned Date must be on or before Submission Date.");
            }

            if (!ModelState.IsValid)
            {
                await LoadDropDowns(task);
                return View(task);
            }

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

            // allow owner or admin
            if (!IsAdmin())
            {
                var userId = GetCurrentUserId();
                if (userId == null || task.AssignedPersonId != userId.Value)
                    return Forbid();
            }

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

            // authorization: only owner or admin
            if (!IsAdmin())
            {
                var userId = GetCurrentUserId();
                if (userId == null || task.AssignedPersonId != userId.Value)
                    return Forbid();
            }

            return View(task);
        }

        // DELETE - POST
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var task = await _context.Tasks.FindAsync(id);
            if (task != null)
            {
                // authorization: only owner or admin
                if (!IsAdmin())
                {
                    var userId = GetCurrentUserId();
                    if (userId == null || task.AssignedPersonId != userId.Value)
                        return Forbid();
                }

                _context.Tasks.Remove(task);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // PROFILE - show current user's profile (user info only)
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("SignIn");

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId.Value);

            if (user == null) return NotFound();

            return View(user);
        }

        // Helper - load dropdowns (filters users for non-admins)
        private async Task LoadDropDowns(TaskModel? task = null)
        {
            var usersQuery = _context.Users.OrderBy(u => u.Name).AsQueryable();
            if (!IsAdmin())
            {
                usersQuery = usersQuery.Where(u => u.Role == RoleType.User);
            }

            var users = await usersQuery.ToListAsync();
                          
            var categories = await _context.Categories
                .OrderBy(c => c.Name)
                .ToListAsync();

            ViewBag.Users = new SelectList(users, "Id", "Name", task?.AssignedPersonId);
            ViewBag.Categories = new SelectList(categories, "Name", "Name", task?.CategoryName);

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