using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Lasamify.Data;
using Lasamify.Models;

namespace Lasamify.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? search, string? category)
        {
            var query = _context.Products
                .Include(p => p.Seller)
                .Where(p => p.IsAvailable && p.Stock > 0)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(p => p.Name.Contains(search) || p.Description.Contains(search));

            if (!string.IsNullOrWhiteSpace(category))
                query = query.Where(p => p.Category == category);

            var vm = new ProductSearchViewModel
            {
                SearchQuery = search,
                Category = category,
                Products = await query.OrderByDescending(p => p.CreatedAt).ToListAsync()
            };

            return View(vm);
        }

        public IActionResult About() => View();

        public IActionResult Landing() => View();
    }
}
