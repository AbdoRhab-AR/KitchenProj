using KitchenProject.Classes;
using KitchenProject.Models.Entities;
using KitchenProject.Models.Enums;
using KitchenProject.Models.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace KitchenProject.Controllers
{
	[ViewLayout("_DashboardLayout")]

	// طلبات المواد الداخلية: طلب (مهندس/مصمم/مخزن) ← اعتماد المدير ← صرف من المخزن، أو تحويل النقص لفاتورة شراء
	public class MaterialRequestsController : Controller
	{
		private readonly IUnitOfWork<MaterialRequest> _request;
		private readonly IUnitOfWork<Item> _item;
		private readonly IUnitOfWork<StockTransaction> _tx;
		private readonly IUnitOfWork<Purchase> _purchase;
		private readonly IUnitOfWork<Project> _project;
		private readonly LookupService _lookup;
		private readonly StockService _stock;

		public MaterialRequestsController(IUnitOfWork<MaterialRequest> request,
										  IUnitOfWork<Item> item,
										  IUnitOfWork<StockTransaction> tx,
										  IUnitOfWork<Purchase> purchase,
										  IUnitOfWork<Project> project,
										  LookupService lookup,
										  StockService stock)
		{
			_request = request;
			_item = item;
			_tx = tx;
			_purchase = purchase;
			_project = project;
			_lookup = lookup;
			_stock = stock;
		}

		private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

		// المدير والمخزن والمحاسب يرون كل الطلبات، والباقي يرى طلباته فقط
		private bool SeesAll => AppRoles.CanAccess(User, "MaterialRequests", "Issue") || User.IsInRole(AppRoles.Accountant);

		// GET: MaterialRequests
		public async Task<IActionResult> Index(MaterialRequestStatus? status, Guid? projectId, int page = 1)
		{
			ViewBag.ActivePage = "MaterialRequests";
			ViewBag.Status = status;
			ViewBag.ProjectId = projectId;

			var query = _request.Entity.Include(r => r.Project, r => r.Warehouse, r => r.RequestedBy!, r => r.RequestedBy!.UserProfile, r => r.Items);
			if (!SeesAll)
			{
				var me = CurrentUserId;
				query = query.Where(r => r.RequestedById == me);
			}
			if (status.HasValue) query = query.Where(r => r.Status == status.Value);
			if (projectId.HasValue) query = query.Where(r => r.ProjectId == projectId.Value);

			var totalCount = await query.CountAsync();
			int pageSize = 15;
			var requests = await query.OrderByDescending(r => r.Created)
									  .Skip((page - 1) * pageSize)
									  .Take(pageSize)
									  .ToListAsync();

			ViewBag.CurrentPage = page;
			ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
			ViewBag.TotalCount = totalCount;
			ViewBag.StatusList = LookupService.EnumList(status);
			ViewBag.Projects = await _lookup.ProjectsAsync(projectId);

			return View(requests);
		}

		// GET: MaterialRequests/Details/5
		public async Task<IActionResult> Details(Guid? id)
		{
			ViewBag.ActivePage = "MaterialRequests";

			var request = await LoadFullAsync(id);
			if (request == null || !CanSee(request))
			{
				return NotFound();
			}

			ViewBag.Balances = await _stock.BalancesAsync(request.WarehouseId);
			ViewBag.Suppliers = await _lookup.SuppliersAsync(null);
			return View(request);
		}

		// GET: MaterialRequests/Create?projectId=5
		public async Task<IActionResult> Create(Guid? projectId)
		{
			ViewBag.ActivePage = "MaterialRequests";

			var model = new MaterialRequest
			{
				ProjectId = projectId ?? Guid.Empty,
				WarehouseId = await _lookup.DefaultWarehouseIdAsync(),
				Items = { new MaterialRequestItem() }
			};

			await FillFormListsAsync(model);
			return View(model);
		}

		// POST: MaterialRequests/Create
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(MaterialRequest model)
		{
			ViewBag.ActivePage = "MaterialRequests";

			model.Items = model.Items.Where(l => l.ItemId != Guid.Empty).ToList();
			foreach (var key in ModelState.Keys.Where(k => k.StartsWith("Items[")).ToList())
			{
				ModelState.Remove(key);
			}

			if (model.ProjectId == Guid.Empty)
				ModelState.AddModelError(nameof(model.ProjectId), "يجب اختيار المشروع");
			if (model.Items.Count == 0)
				ModelState.AddModelError("", "يجب إضافة صنف واحد على الأقل");
			if (model.Items.Any(l => l.Quantity <= 0))
				ModelState.AddModelError("", "الكميات يجب أن تكون أكبر من صفر");
			if (model.Items.GroupBy(l => l.ItemId).Any(g => g.Count() > 1))
				ModelState.AddModelError("", "الصنف مكرر في أكثر من سطر، اجمع الكمية في سطر واحد");

			var ids = model.Items.Select(l => l.ItemId).Distinct().ToList();
			if (await _item.Entity.GetWhere(i => ids.Contains(i.Id) && i.Type == ItemType.Raw).CountAsync() != ids.Count)
				ModelState.AddModelError("", "الطلب يكون للأصناف الخام فقط");

			if (ModelState.IsValid)
			{
				model.Id = Guid.NewGuid();
				model.Created = DateTime.UtcNow;
				model.RequestNo = await GenerateNoAsync();
				model.RequestedById = CurrentUserId;
				model.RequestDate = DateTime.Today;
				model.Status = MaterialRequestStatus.Pending;

				var order = 0;
				foreach (var line in model.Items)
				{
					line.Id = Guid.NewGuid();
					line.MaterialRequestId = model.Id;
					line.Created = model.Created.AddTicks(order++);
				}

				_request.Entity.Insert(model);
				await _request.SaveAsync();

				TempData["Success"] = $"تم إرسال طلب المواد {model.RequestNo} للاعتماد";
				return RedirectToAction(nameof(Details), new { id = model.Id });
			}

			await FillFormListsAsync(model);
			return View(model);
		}

		// POST: MaterialRequests/Approve/5  (المدير فقط)
		[HttpPost]
		[ValidateAntiForgeryToken]
		public Task<IActionResult> Approve(Guid id, string? decisionNotes) =>
			DecideAsync(id, MaterialRequestStatus.Approved, decisionNotes);

		// POST: MaterialRequests/Reject/5  (المدير فقط)
		[HttpPost]
		[ValidateAntiForgeryToken]
		public Task<IActionResult> Reject(Guid id, string? decisionNotes) =>
			DecideAsync(id, MaterialRequestStatus.Rejected, decisionNotes);

		// POST: MaterialRequests/Cancel/5  (صاحب الطلب أو المدير، قبل الصرف أو الشراء)
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Cancel(Guid id)
		{
			var request = await _request.Entity.GetByIdAsync(id);
			if (request == null || !CanSee(request))
			{
				return NotFound();
			}

			if (request.RequestedById != CurrentUserId && !AppRoles.IsFullAccess(User))
			{
				TempData["Error"] = "يمكن لصاحب الطلب أو المدير فقط إلغاؤه";
			}
			else if (request.Status is not (MaterialRequestStatus.Pending or MaterialRequestStatus.Approved))
			{
				TempData["Error"] = "لا يمكن إلغاء طلب تم صرفه أو تحويله للشراء";
			}
			else
			{
				request.Status = MaterialRequestStatus.Cancelled;
				request.Modified = DateTime.UtcNow;
				await _request.SaveAsync();
				TempData["Success"] = $"تم إلغاء الطلب {request.RequestNo}";
			}

			return RedirectToAction(nameof(Details), new { id });
		}

		// POST: MaterialRequests/Issue/5  صرف الطلب المعتمد من المخزن على المشروع
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Issue(Guid id)
		{
			var request = await _request.Entity.Include(r => r.Items).Include(r => r.Items).ThenInclude(l => l.Item)
				.AsTracking()
				.FirstOrDefaultAsync(r => r.Id == id);
			if (request == null)
			{
				return NotFound();
			}

			if (request.Status is not (MaterialRequestStatus.Approved or MaterialRequestStatus.Ordered))
			{
				TempData["Error"] = "الصرف يكون للطلبات المعتمدة فقط";
				return RedirectToAction(nameof(Details), new { id });
			}

			var balances = await _stock.BalancesAsync(request.WarehouseId);
			var shortages = request.Items
				.Where(l => l.Quantity > balances.GetValueOrDefault(l.ItemId))
				.Select(l => $"{l.Item.Name} (المتوفر {balances.GetValueOrDefault(l.ItemId):0.##} {l.Item.Unit})")
				.ToList();
			if (shortages.Any())
			{
				TempData["Error"] = "الرصيد لا يكفي لصرف: " + string.Join("، ", shortages) + ". حوّل النقص لفاتورة شراء أو انتظر استلامها.";
				return RedirectToAction(nameof(Details), new { id });
			}

			var refNo = await DocumentNumbers.NextAsync(
				_tx.Entity.GetWhere(t => t.ReferenceNo != null).Select(t => t.ReferenceNo!), "ISS-");
			var costs = await _stock.AverageCostsAsync();
			var now = DateTime.UtcNow;

			foreach (var line in request.Items)
			{
				_tx.Entity.Insert(new StockTransaction
				{
					Id = Guid.NewGuid(),
					Created = now,
					ItemId = line.ItemId,
					WarehouseId = request.WarehouseId,
					ProjectId = request.ProjectId,
					TransactionType = StockTransactionType.Issue,
					TransactionDate = DateTime.Today,
					QuantityOut = line.Quantity,
					UnitCost = costs.GetValueOrDefault(line.ItemId, line.Item.PurchasePrice),
					ReferenceNo = refNo,
					Notes = $"طلب مواد {request.RequestNo}"
				});
			}

			request.Status = MaterialRequestStatus.Issued;
			request.IssueRef = refNo;
			request.Modified = now;

			await ProjectWorkflow.AdvanceAsync(_project, request.ProjectId, ProjectStatus.InProgress);
			await _request.SaveAsync();

			TempData["Success"] = $"تم صرف الطلب {request.RequestNo} للمشروع (إذن رقم {refNo})";
			return RedirectToAction(nameof(Details), new { id });
		}

		// POST: MaterialRequests/ToPurchase/5  ينشئ فاتورة شراء مسودة بالكميات الناقصة فقط
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> ToPurchase(Guid id, Guid? supplierId)
		{
			var request = await _request.Entity.Include(r => r.Items).Include(r => r.Items).ThenInclude(l => l.Item)
				.AsTracking()
				.FirstOrDefaultAsync(r => r.Id == id);
			if (request == null)
			{
				return NotFound();
			}

			if (request.Status != MaterialRequestStatus.Approved)
			{
				TempData["Error"] = "التحويل للشراء يكون للطلبات المعتمدة فقط";
				return RedirectToAction(nameof(Details), new { id });
			}
			if (supplierId == null || supplierId == Guid.Empty)
			{
				TempData["Error"] = "يجب اختيار المورد";
				return RedirectToAction(nameof(Details), new { id });
			}

			var balances = await _stock.BalancesAsync(request.WarehouseId);
			var lines = request.Items
				.Select(l => new { Line = l, Missing = l.Quantity - Math.Max(0, balances.GetValueOrDefault(l.ItemId)) })
				.Where(x => x.Missing > 0)
				.ToList();
			if (!lines.Any())
			{
				TempData["Error"] = "كل الكميات متوفرة في المخزن، يمكن صرف الطلب مباشرة";
				return RedirectToAction(nameof(Details), new { id });
			}

			var now = DateTime.UtcNow;
			var purchase = new Purchase
			{
				Id = Guid.NewGuid(),
				Created = now,
				PurchaseNo = await DocumentNumbers.NextAsync(_purchase.Entity.GetAll().Select(p => p.PurchaseNo), "PUR-"),
				SupplierId = supplierId.Value,
				WarehouseId = request.WarehouseId,
				ProjectId = request.ProjectId,
				PurchaseDate = DateTime.Today,
				Status = PurchaseStatuses.Draft,
				Notes = $"من طلب المواد {request.RequestNo}"
			};

			var order = 0;
			foreach (var x in lines)
			{
				purchase.Items.Add(new PurchaseItem
				{
					Id = Guid.NewGuid(),
					Created = now.AddTicks(order++),
					PurchaseId = purchase.Id,
					ItemId = x.Line.ItemId,
					Quantity = x.Missing,
					UnitPrice = x.Line.Item.PurchasePrice
				});
			}
			purchase.Recalculate();

			_purchase.Entity.Insert(purchase);
			request.Status = MaterialRequestStatus.Ordered;
			request.PurchaseId = purchase.Id;
			request.Modified = now;
			await _request.SaveAsync();

			TempData["Success"] = $"تم إنشاء فاتورة الشراء {purchase.PurchaseNo} بالكميات الناقصة، راجع الأسعار ثم استلمها بالمخزن";
			return RedirectToAction("Edit", "Purchases", new { id = purchase.Id });
		}

		//---------------------------------------------------------------------

		private async Task<IActionResult> DecideAsync(Guid id, MaterialRequestStatus decision, string? notes)
		{
			var request = await _request.Entity.GetByIdAsync(id);
			if (request == null)
			{
				return NotFound();
			}

			if (request.Status != MaterialRequestStatus.Pending)
			{
				TempData["Error"] = "تم اتخاذ قرار في هذا الطلب مسبقاً";
				return RedirectToAction(nameof(Details), new { id });
			}
			if (decision == MaterialRequestStatus.Rejected && string.IsNullOrWhiteSpace(notes))
			{
				TempData["Error"] = "اكتب سبب الرفض";
				return RedirectToAction(nameof(Details), new { id });
			}

			request.Status = decision;
			request.DecidedById = CurrentUserId;
			request.DecidedAt = DateTime.UtcNow;
			request.DecisionNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
			request.Modified = DateTime.UtcNow;
			await _request.SaveAsync();

			TempData["Success"] = decision == MaterialRequestStatus.Approved
				? $"تم اعتماد الطلب {request.RequestNo}، وهو الآن بانتظار الصرف من المخزن"
				: $"تم رفض الطلب {request.RequestNo}";
			return RedirectToAction(nameof(Details), new { id });
		}

		private bool CanSee(MaterialRequest request) => SeesAll || request.RequestedById == CurrentUserId;

		private Task<MaterialRequest?> LoadFullAsync(Guid? id)
		{
			if (id == null)
			{
				return Task.FromResult<MaterialRequest?>(null);
			}

			return _request.Entity.Include(r => r.Project, r => r.Project.Customer, r => r.Warehouse, r => r.Purchase!)
				.Include(r => r.RequestedBy!).ThenInclude(u => u.UserProfile)
				.Include(r => r.DecidedBy!).ThenInclude(u => u.UserProfile)
				.Include(r => r.Items).ThenInclude(l => l.Item)
				.AsSplitQuery()
				.FirstOrDefaultAsync(r => r.Id == id)!;
		}

		private Task<string> GenerateNoAsync() =>
			DocumentNumbers.NextAsync(_request.Entity.GetAll().Select(r => r.RequestNo), "MR-");

		private async Task FillFormListsAsync(MaterialRequest model)
		{
			ViewBag.Projects = await _lookup.ProjectsAsync(model.ProjectId == Guid.Empty ? null : model.ProjectId);
			ViewBag.Warehouses = await _lookup.WarehousesAsync(model.WarehouseId);
			ViewBag.ItemOptions = await _lookup.ItemOptionsAsync(ItemType.Raw);
			ViewBag.Balances = await _stock.BalancesAsync(model.WarehouseId);
		}
	}
}
