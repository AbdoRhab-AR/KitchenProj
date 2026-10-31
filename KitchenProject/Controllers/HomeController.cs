using KitchenProject.Classes;
using KitchenProject.Models.Entities;
using KitchenProject.Models.Enums;
using KitchenProject.Models.Interfaces;
using KitchenProject.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace KitchenProject.Controllers
{
    public class HomeController : Controller
    {
        private readonly IUnitOfWork<SiteState> _siteState;
        private readonly IUnitOfWork<Project> _project;
        private readonly IUnitOfWork<SiteVisit> _visit;
        private readonly IUnitOfWork<CustomerPayment> _payment;
        private readonly IUnitOfWork<Expense> _expense;
        private readonly IUnitOfWork<Contract> _contract;
        private readonly IUnitOfWork<StockTransaction> _tx;
        private readonly IUnitOfWork<Item> _item;
        private readonly IUnitOfWork<Purchase> _purchase;
        private readonly IUnitOfWork<MaterialRequest> _request;
        private readonly StockService _stock;

        public HomeController(IUnitOfWork<SiteState> siteState,
                              IUnitOfWork<Project> project,
                              IUnitOfWork<SiteVisit> visit,
                              IUnitOfWork<CustomerPayment> payment,
                              IUnitOfWork<Expense> expense,
                              IUnitOfWork<Contract> contract,
                              IUnitOfWork<StockTransaction> tx,
                              IUnitOfWork<Item> item,
                              IUnitOfWork<Purchase> purchase,
                              IUnitOfWork<MaterialRequest> request,
                              StockService stock)
        {
            _siteState = siteState;
            _project = project;
            _visit = visit;
            _payment = payment;
            _expense = expense;
            _contract = contract;
            _tx = tx;
            _item = item;
            _purchase = purchase;
            _request = request;
            _stock = stock;
        }

        // GET: Home  لوحة التحكم
        [ViewLayout("_DashboardLayout")]
        public async Task<IActionResult> Index()
        {
            ViewBag.ActivePage = "Home";

            var me = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var today = DateTime.Today;
            var monthStart = new DateTime(today.Year, today.Month, 1);
            var model = new DashboardViewModel();

            model.ProjectsByStatus = await _project.Entity.GetAll()
                .GroupBy(p => p.Status)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count);
            model.ActiveProjects = model.ProjectsByStatus
                .Where(s => s.Key is not (ProjectStatus.Closed or ProjectStatus.Cancelled or ProjectStatus.Delivered))
                .Sum(s => s.Value);

            model.RecentProjects = await _project.Entity.Include(p => p.Customer)
                .OrderByDescending(p => p.Created).Take(6).ToListAsync();

            // المشاريع المسندة للمستخدم (مبيعات / مهندس / مصمم) ولم تُغلق بعد
            model.MyProjects = await _project.Entity.Include(p => p.Customer)
                .Where(p => (p.SalesUserId == me || p.EngineerUserId == me || p.DesignerUserId == me)
                         && p.Status != ProjectStatus.Closed && p.Status != ProjectStatus.Cancelled)
                .OrderBy(p => p.ExpectedDeliveryDate ?? DateTime.MaxValue).Take(6).ToListAsync();

            if (AppRoles.CanAccess(User, "SiteVisits"))
            {
                var visits = _visit.Entity.Include(v => v.Project, v => v.Project.Customer, v => v.Engineer)
                    .Where(v => v.Status == SiteVisitStatus.Scheduled && v.VisitDate >= today && v.VisitDate < today.AddDays(8));
                if (!AppRoles.IsFullAccess(User) && User.IsInRole(AppRoles.Engineer))
                {
                    visits = visits.Where(v => v.EngineerId == me);
                }
                model.UpcomingVisits = await visits.OrderBy(v => v.VisitDate).Take(6).ToListAsync();
            }

            model.ShowFinance = AppRoles.CanAccess(User, "Payments");
            if (model.ShowFinance)
            {
                model.MonthCollections = await _payment.Entity.GetWhere(p => p.PaymentDate >= monthStart).SumAsync(p => (decimal?)p.Amount) ?? 0;
                model.MonthExpenses = await _expense.Entity.GetWhere(e => e.ExpenseDate >= monthStart).SumAsync(e => (decimal?)e.Amount) ?? 0;
                model.Outstanding = await _contract.Entity
                    .GetWhere(c => c.Status == ContractStatuses.Active || c.Status == ContractStatuses.Completed)
                    .SumAsync(c => (decimal?)(c.TotalAmount - c.PaidAmount)) ?? 0;
                model.MonthContracts = await _contract.Entity
                    .GetWhere(c => c.Status != ContractStatuses.Cancelled && c.Created >= monthStart.ToUniversalTime())
                    .SumAsync(c => (decimal?)c.TotalAmount) ?? 0;
            }

            model.ShowStock = AppRoles.CanAccess(User, "Stock");
            if (model.ShowStock)
            {
                var balances = await _stock.BalancesAsync();
                var watched = await _item.Entity.GetWhere(i => i.IsActive && i.Type == ItemType.Raw && i.MinQuantity > 0)
                    .Select(i => new { i.Id, i.MinQuantity }).ToListAsync();
                model.LowStockCount = watched.Count(i => balances.GetValueOrDefault(i.Id) <= i.MinQuantity);
                model.MonthMaterialsIssued = await _tx.Entity
                    .GetWhere(t => t.ProjectId != null && t.TransactionDate >= monthStart)
                    .SumAsync(t => (decimal?)((t.QuantityOut - t.QuantityIn) * t.UnitCost)) ?? 0;
                if (AppRoles.CanAccess(User, "Purchases"))
                {
                    model.DraftPurchases = await _purchase.Entity.GetWhere(p => p.Status == PurchaseStatuses.Draft).CountAsync();
                }
            }

            model.ShowRequests = AppRoles.CanAccess(User, "MaterialRequests");
            if (model.ShowRequests)
            {
                if (AppRoles.IsFullAccess(User))
                {
                    model.RequestsStatus = MaterialRequestStatus.Pending;
                    model.RequestsLabel = "طلبات مواد بانتظار اعتمادك";
                    model.RequestsCount = await _request.Entity.GetWhere(r => r.Status == MaterialRequestStatus.Pending).CountAsync();
                }
                else if (AppRoles.CanAccess(User, "MaterialRequests", "Issue"))
                {
                    model.RequestsStatus = MaterialRequestStatus.Approved;
                    model.RequestsLabel = "طلبات معتمدة بانتظار الصرف";
                    model.RequestsCount = await _request.Entity.GetWhere(r => r.Status == MaterialRequestStatus.Approved || r.Status == MaterialRequestStatus.Ordered).CountAsync();
                }
                else
                {
                    model.RequestsLabel = "طلباتي المفتوحة";
                    model.RequestsCount = await _request.Entity
                        .GetWhere(r => r.RequestedById == me && (r.Status == MaterialRequestStatus.Pending || r.Status == MaterialRequestStatus.Approved || r.Status == MaterialRequestStatus.Ordered))
                        .CountAsync();
                }
            }

            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        // GET: Home/Closing  (تظهر حتى لغير المسجلين)
        [AllowAnonymous]
        public async Task<IActionResult> Closing()
        {
            var siteState = await _siteState.Entity.GetAll().FirstOrDefaultAsync();

            // الموقع مفتوح، لا داعي لصفحة الإغلاق
            if (siteState == null || siteState.State)
            {
                return RedirectToAction(nameof(Index));
            }

            return View(siteState);
        }
    }
}
