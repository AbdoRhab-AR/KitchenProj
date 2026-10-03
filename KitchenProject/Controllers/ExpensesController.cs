using KitchenProject.Classes;
using KitchenProject.Models;
using KitchenProject.Models.Entities;
using KitchenProject.Models.Enums;
using KitchenProject.Models.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace KitchenProject.Controllers
{
    [ViewLayout("_DashboardLayout")]
    public class ExpensesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IUnitOfWork<Expense> _expense;

        public ExpensesController(ApplicationDbContext context, IUnitOfWork<Expense> expense)
        {
            _context = context;
            _expense = expense;
        }

        // GET: Expenses
        public async Task<IActionResult> Index(string? searchString, string? typeFilter, Guid? projectFilter, int page = 1)
        {
            ViewBag.ActivePage = "Expenses";
            ViewBag.SearchString = searchString;
            ViewBag.TypeFilter = typeFilter;
            ViewBag.ProjectFilter = projectFilter;

            var query = _context.Expenses
                .Include(e => e.Project)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                searchString = searchString.Trim();
                query = query.Where(e =>
                    (e.Description != null && e.Description.Contains(searchString)) ||
                    (e.Project != null && (e.Project.ProjectNo.Contains(searchString) || e.Project.ProjectName.Contains(searchString))));
            }

            if (!string.IsNullOrWhiteSpace(typeFilter))
                query = query.Where(e => e.ExpenseType == typeFilter);

            if (projectFilter.HasValue)
                query = query.Where(e => e.ProjectId == projectFilter.Value);

            var totalCount = await query.CountAsync();
            int pageSize = 10;
            var expenses = await query
                .OrderByDescending(e => e.Created)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            ViewBag.TotalCount = totalCount;

            ViewBag.TotalExpensesAmount = await _context.Expenses.SumAsync(e => e.Amount);

            var projects = await _context.Projects
                .Include(p => p.Customer)
                .OrderByDescending(p => p.Created)
                .Select(p => new { Id = p.Id, Name = $"{p.ProjectNo} - {p.ProjectName}" })
                .ToListAsync();
            ViewBag.ProjectsList = new SelectList(projects, "Id", "Name", projectFilter);

            var expenseTypes = await _context.Expenses
                .Select(e => e.ExpenseType)
                .Distinct()
                .ToListAsync();
            ViewBag.ExpenseTypesList = new SelectList(expenseTypes, typeFilter);

            return View(expenses);
        }

        // GET: Expenses/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            ViewBag.ActivePage = "Expenses";

            if (id == null) return NotFound();

            var expense = await _context.Expenses
                .Include(e => e.Project)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (expense == null) return NotFound();

            return View(expense);
        }

        // GET: Expenses/Create
        public async Task<IActionResult> Create(Guid? projectId)
        {
            ViewBag.ActivePage = "Expenses";

            var model = new Expense
            {
                ProjectId = projectId,
                Payment = PaymentMethod.Cash,
                ExpenseType = "مصروفات عامة"
            };

            await PopulateDropdownsAsync(model.ProjectId, model.ExpenseType);
            return View(model);
        }

        // POST: Expenses/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Expense expense, IFormFile? AttachmentFile)
        {
            ViewBag.ActivePage = "Expenses";

            if (ModelState.IsValid)
            {
                expense.Id = Guid.NewGuid();
                expense.Created = DateTime.UtcNow;

                if (AttachmentFile != null && AttachmentFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "expenses");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                    
                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + AttachmentFile.FileName;
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await AttachmentFile.CopyToAsync(stream);
                    }
                    
                    expense.AttachmentPath = "/uploads/expenses/" + uniqueFileName;
                }

                _expense.Entity.Insert(expense);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"تم إضافة المصروف بقيمة {expense.Amount} بنجاح";
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropdownsAsync(expense.ProjectId, expense.ExpenseType);
            return View(expense);
        }

        // GET: Expenses/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            ViewBag.ActivePage = "Expenses";

            if (id == null) return NotFound();

            var expense = await _expense.Entity.GetByIdAsync(id.Value);
            if (expense == null) return NotFound();

            await PopulateDropdownsAsync(expense.ProjectId, expense.ExpenseType);
            return View(expense);
        }

        // POST: Expenses/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, Expense expense, IFormFile? AttachmentFile)
        {
            ViewBag.ActivePage = "Expenses";

            if (id != expense.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    if (AttachmentFile != null && AttachmentFile.Length > 0)
                    {
                        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "expenses");
                        if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                        
                        var uniqueFileName = Guid.NewGuid().ToString() + "_" + AttachmentFile.FileName;
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                        
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await AttachmentFile.CopyToAsync(stream);
                        }
                        
                        expense.AttachmentPath = "/uploads/expenses/" + uniqueFileName;
                    }

                    expense.Modified = DateTime.UtcNow;
                    _expense.Entity.Update(expense);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "تم تحديث بيانات المصروف بنجاح";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (await _expense.Entity.GetByIdAsync(expense.Id) == null) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropdownsAsync(expense.ProjectId, expense.ExpenseType);
            return View(expense);
        }

        // POST: Expenses/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var expense = await _expense.Entity.GetByIdAsync(id);
            if (expense != null)
            {
                _expense.Entity.Delete(expense);
                await _context.SaveChangesAsync();
                TempData["Success"] = "تم حذف المصروف بنجاح";
            }
            else
            {
                TempData["Error"] = "تعذر العثور على المصروف المراد حذفه";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateDropdownsAsync(Guid? projectId, string? expenseType)
        {
            var projects = await _context.Projects
                .Include(p => p.Customer)
                .OrderByDescending(p => p.Created)
                .Select(p => new { Id = p.Id, Name = $"{p.ProjectNo} - {p.ProjectName} ({p.Customer.Name})" })
                .ToListAsync();

            ViewBag.ProjectId = new SelectList(projects, "Id", "Name", projectId);

            var enumList = Enum.GetValues(typeof(PaymentMethod)).Cast<PaymentMethod>().Select(e => new
            {
                Id = (int)e,
                Name = e.GetType().GetMember(e.ToString()).First().GetCustomAttributes(false).OfType<System.ComponentModel.DataAnnotations.DisplayAttribute>().LastOrDefault()?.Name ?? e.ToString()
            });
            ViewBag.Payment = new SelectList(enumList, "Id", "Name");
            
            // Common expense types
            var commonTypes = new List<string> { "مصروفات عامة", "نثريات", "وقود ومواصلات", "مواد خام (شراء مباشر)", "مكافآت", "صيانة", "أخرى" };
            var dbTypes = await _context.Expenses.Select(e => e.ExpenseType).Distinct().ToListAsync();
            var allTypes = commonTypes.Union(dbTypes).Where(t => !string.IsNullOrEmpty(t)).ToList();
            ViewBag.ExpenseTypes = new SelectList(allTypes, expenseType);
        }
    }
}
