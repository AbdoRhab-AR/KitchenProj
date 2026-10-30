using KitchenProject.Models.Entities;
using KitchenProject.Models.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KitchenProject.Controllers
{
    public class HomeController : Controller
    {
        private readonly IUnitOfWork<SiteState> _siteState;

        public HomeController(IUnitOfWork<SiteState> siteState)
        {
            _siteState = siteState;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        // GET: Home/Closing
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
