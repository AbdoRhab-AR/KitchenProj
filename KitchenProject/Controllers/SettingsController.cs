using KitchenProject.Classes;
using KitchenProject.Models.Entities;
using KitchenProject.Models.Interfaces;
using KitchenProject.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KitchenProject.Controllers
{
	[ViewLayout("_DashboardLayout")]
	//[Authorize(Policy = "SettingsPolicy")]

	public class SettingsController : Controller
	{
		private readonly IUnitOfWork<SiteInfo> _siteInfo;
		private readonly IUnitOfWork<Contact> _contact;
		private readonly IWebHostEnvironment _env;

		public SettingsController(IUnitOfWork<SiteInfo> siteInfo,
								  IUnitOfWork<Contact> contact,
								  IWebHostEnvironment env)
		{
			_siteInfo = siteInfo;
			_contact = contact;
			_env = env;
		}

		// GET: Settings
		public async Task<IActionResult> Index()
		{
			ViewBag.ActivePage = "Settings";

			var site = await _siteInfo.Entity.GetAll().FirstOrDefaultAsync();
			var contact = await _contact.Entity.GetAll().FirstOrDefaultAsync();
			if (site == null || contact == null)
			{
				return NotFound();
			}

			return View(new SettingsViewModel
			{
				Name = site.Name,
				Activity = site.Activity,
				About = site.About,
				LogoUrl = site.LogoUrl,
				LetterheadUrl = site.LetterheadUrl,
				DefaultQuoteTerms = site.DefaultQuoteTerms,
				Email = contact.Email,
				Phone = contact.Phone
			});
		}

		// POST: Settings
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Index(SettingsViewModel model)
		{
			ViewBag.ActivePage = "Settings";

			var site = await _siteInfo.Entity.GetAll().FirstOrDefaultAsync();
			var contact = await _contact.Entity.GetAll().FirstOrDefaultAsync();
			if (site == null || contact == null)
			{
				return NotFound();
			}

			// الصور الحالية تؤخذ من قاعدة البيانات وليس من النموذج
			model.LogoUrl = site.LogoUrl;
			model.LetterheadUrl = site.LetterheadUrl;

			var filesToDelete = new List<string?>();

			if (ModelState.IsValid && model.Logo != null)
			{
				var (url, error) = await FileUploads.SaveAsync(model.Logo, _env, "branding", FileUploads.Images, maxMb: 5);
				if (error != null) ModelState.AddModelError(nameof(model.Logo), error);
				else { filesToDelete.Add(site.LogoUrl); site.LogoUrl = url; }
			}

			if (ModelState.IsValid && model.Letterhead != null)
			{
				var (url, error) = await FileUploads.SaveAsync(model.Letterhead, _env, "branding", FileUploads.Images, maxMb: 5);
				if (error != null) ModelState.AddModelError(nameof(model.Letterhead), error);
				else { filesToDelete.Add(site.LetterheadUrl); site.LetterheadUrl = url; }
			}
			else if (model.RemoveLetterhead)
			{
				filesToDelete.Add(site.LetterheadUrl);
				site.LetterheadUrl = null;
			}

			if (!ModelState.IsValid)
			{
				return View(model);
			}

			site.Name = model.Name.Trim();
			site.Activity = model.Activity.Trim();
			site.About = model.About.Trim();
			site.DefaultQuoteTerms = string.IsNullOrWhiteSpace(model.DefaultQuoteTerms) ? null : model.DefaultQuoteTerms.Trim();
			site.Modified = DateTime.UtcNow;
			_siteInfo.Entity.Update(site);

			contact.Email = model.Email.Trim();
			contact.Phone = model.Phone.Trim();
			contact.Modified = DateTime.UtcNow;
			_contact.Entity.Update(contact);

			await _siteInfo.SaveAsync();

			// FileUploads.Delete يتجاهل صور الهوية الافتراضية في /images/brand
			filesToDelete.ForEach(f => FileUploads.Delete(f, _env));

			TempData["Success"] = "تم حفظ إعدادات الشركة بنجاح";
			return RedirectToAction(nameof(Index));
		}
	}
}
