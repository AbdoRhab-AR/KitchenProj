using KitchenProject.Classes;
using KitchenProject.Models;
using KitchenProject.Models.Entities;
using KitchenProject.Models.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KitchenProject.Controllers
{
	[ViewLayout("_DashboardLayout")]
	//[Authorize(Policy = "ProgOrAdminOrEmployeePolicy")]

	public class CustomersController : Controller
    {
		private readonly ApplicationDbContext _context;
		private readonly IUnitOfWork<Customer> _customer;

		public CustomersController(ApplicationDbContext context,
						  IUnitOfWork<Customer> customer,
						  IWebHostEnvironment host)
		{
			_context = context;
			_customer = customer;
		}

		// GET: Customers
		public async Task<IActionResult> Index(string? searchString, int page = 1)
        {
            ViewBag.ActivePage = "Customers";
            ViewBag.SearchString = searchString;

            var query = _customer.Entity.Include(c => c.Projects);

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                searchString = searchString.Trim();
                query = query.Where(c => c.Name.Contains(searchString)
                                      || c.Phone.Contains(searchString)
                                      || c.CustomerNo.Contains(searchString)
                                      || (c.City != null && c.City.Contains(searchString)));
            }

            var totalCount = await query.CountAsync();
            int pageSize = 10;
            var customers = await query.OrderByDescending(c => c.Created)
                                      .Skip((page - 1) * pageSize)
                                      .Take(pageSize)
                                      .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            ViewBag.TotalCount = totalCount;

            // Metrics for Dashboard header cards
            var firstDayOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            ViewBag.TotalCustomersCount = await _customer.Entity.GetAll().CountAsync();
            ViewBag.NewCustomersThisMonth = await _customer.Entity.GetAll()
                .Where(c => c.Created >= firstDayOfMonth)
                .CountAsync();

            return View(customers);
        }

        // GET: Customers/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            ViewBag.ActivePage = "Customers";

            if (id == null)
            {
                return NotFound();
            }

            var customer = await _customer.Entity.Include(c => c.Projects)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (customer == null)
            {
                return NotFound();
            }

            return View(customer);
        }

        // GET: Customers/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.ActivePage = "Customers";

            // Generate unique customer number
            var count = await _customer.Entity.GetAll().CountAsync();
            var nextNo = $"CUST-{(count + 1):D4}";

            var model = new Customer
            {
                CustomerNo = nextNo,
                Name = "",
                Phone = ""
            };

            return View(model);
        }

        // POST: Customers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Customer customer)
        {
            ViewBag.ActivePage = "Customers";

            if (ModelState.IsValid)
            {
                customer.Id = Guid.NewGuid();
                customer.Created = DateTime.UtcNow;

                _customer.Entity.Insert(customer);
                await _customer.SaveAsync();

                TempData["Success"] = $"تم إضافة العميل \"{customer.Name}\" بنجاح برقم {customer.CustomerNo}";
                return RedirectToAction(nameof(Index));
            }

            return View(customer);
        }

        // GET: Customers/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            ViewBag.ActivePage = "Customers";

            if (id == null)
            {
                return NotFound();
            }

            var customer = await _customer.Entity.GetByIdAsync(id.Value);
            if (customer == null)
            {
                return NotFound();
            }

            return View(customer);
        }

        // POST: Customers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, Customer customer)
        {
            ViewBag.ActivePage = "Customers";

            if (id != customer.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    customer.Modified = DateTime.UtcNow;
                    _customer.Entity.Update(customer);
                    await _customer.SaveAsync();

                    TempData["Success"] = $"تم تحديث بيانات العميل \"{customer.Name}\" بنجاح";
                }
                catch (DbUpdateConcurrencyException)
                {
                    var exists = await _customer.Entity.GetByIdAsync(customer.Id) != null;
                    if (!exists)
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(customer);
        }

        // POST: Customers/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var hasProjects = await _context.Projects.AnyAsync(p => p.CustomerId == id);
            if (hasProjects)
            {
                TempData["Error"] = "لا يمكن حذف العميل لوجود مشاريع أو دفعات مالية مسجلة باسمه.";
                return RedirectToAction(nameof(Index));
            }

            var customer = await _customer.Entity.GetByIdAsync(id);
            if (customer != null)
            {
                _customer.Entity.Delete(customer);
                await _customer.SaveAsync();
                TempData["Success"] = $"تم حذف العميل \"{customer.Name}\" بنجاح";
            }
            else
            {
                TempData["Error"] = "تعذر العثور على العميل المراد حذفه";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
