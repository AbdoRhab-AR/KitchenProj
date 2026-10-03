using KitchenProject.Models;
using KitchenProject.Models.Enums;
using KitchenProject.Classes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace KitchenProject.Controllers
{
    [ViewLayout("_DashboardLayout")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.ActivePage = "Dashboard";

            var now = DateTime.UtcNow;
            var startOfMonth = new DateTime(now.Year, now.Month, 1);

            // Customers Stats
            var totalCustomers = await _context.Customers.CountAsync();
            var newCustomersThisMonth = await _context.Customers.CountAsync(c => c.Created >= startOfMonth);

            // Projects Stats
            var totalProjects = await _context.Projects.CountAsync();
            var activeProjects = await _context.Projects.CountAsync(p => 
                p.Status == ProjectStatus.InProgress || 
                p.Status == ProjectStatus.Contracted || 
                p.Status == ProjectStatus.Design || 
                p.Status == ProjectStatus.SiteVisit);
            var completedProjects = await _context.Projects.CountAsync(p => 
                p.Status == ProjectStatus.Delivered || 
                p.Status == ProjectStatus.Closed);

            // Financial Stats (Customer Payments)
            var totalRevenues = await _context.CustomerPayments.SumAsync(cp => cp.Amount);
            var revenuesThisMonth = await _context.CustomerPayments
                .Where(cp => cp.Created >= startOfMonth)
                .SumAsync(cp => cp.Amount);

            // Financial Stats (Expenses)
            var totalExpenses = await _context.Expenses.SumAsync(e => e.Amount);
            var expensesThisMonth = await _context.Expenses
                .Where(e => e.Created >= startOfMonth)
                .SumAsync(e => e.Amount);

            // Supplier Payments
            var totalSupplierPayments = await _context.SupplierPayments.SumAsync(sp => sp.Amount);

            // Site Visits Stats
            var pendingVisits = await _context.SiteVisits
                .CountAsync(v => v.Status == SiteVisitStatus.Scheduled);
            
            // Expected Final Amounts from Projects
            var expectedRevenue = await _context.Projects.SumAsync(p => p.FinalAmount);
            
            // Model for the view
            var model = new DashboardViewModel
            {
                TotalCustomers = totalCustomers,
                NewCustomersThisMonth = newCustomersThisMonth,
                TotalProjects = totalProjects,
                ActiveProjects = activeProjects,
                CompletedProjects = completedProjects,
                TotalRevenues = totalRevenues,
                RevenuesThisMonth = revenuesThisMonth,
                TotalExpenses = totalExpenses + totalSupplierPayments, 
                ExpensesThisMonth = expensesThisMonth, 
                PendingVisits = pendingVisits,
                ExpectedRevenue = expectedRevenue,
                RemainingRevenue = expectedRevenue - totalRevenues
            };

            return View(model);
        }
    }

    public class DashboardViewModel
    {
        public int TotalCustomers { get; set; }
        public int NewCustomersThisMonth { get; set; }
        
        public int TotalProjects { get; set; }
        public int ActiveProjects { get; set; }
        public int CompletedProjects { get; set; }

        public decimal TotalRevenues { get; set; }
        public decimal RevenuesThisMonth { get; set; }
        
        public decimal TotalExpenses { get; set; }
        public decimal ExpensesThisMonth { get; set; }

        public int PendingVisits { get; set; }

        public decimal ExpectedRevenue { get; set; }
        public decimal RemainingRevenue { get; set; }
    }
}
