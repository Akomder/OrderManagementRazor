using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrderManagementRazor.Models;

namespace OrderManagementRazor.Pages.Reports
{
    [Authorize]
    public class BestItemsModel : PageModel
    {
        private readonly AppDbContext _context;

        public BestItemsModel(AppDbContext context)
        {
            _context = context;
        }

        public List<BestSellingProduct> BestProducts { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public DateTime? StartDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? EndDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public int TopCount { get; set; } = 10;

        public async Task OnGetAsync()
        {
            var query = _context.OrderDetails
                .Include(od => od.Product)
                .Include(od => od.Order)
                .AsQueryable();

            // Apply date filters
            if (StartDate.HasValue)
            {
                query = query.Where(od => od.Order!.OrderDate >= StartDate.Value);
            }

            if (EndDate.HasValue)
            {
                query = query.Where(od => od.Order!.OrderDate <= EndDate.Value);
            }

            BestProducts = await query
                .GroupBy(od => new { od.ProductId, od.Product!.ProductName, od.Product.Category })
                .Select(g => new BestSellingProduct
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.ProductName,
                    Category = g.Key.Category ?? "N/A",
                    TotalQuantitySold = g.Sum(od => od.Quantity),
                    TotalRevenue = g.Sum(od => od.LineTotal),
                    OrderCount = g.Count()
                })
                .OrderByDescending(p => p.TotalQuantitySold)
                .Take(TopCount)
                .ToListAsync();
        }

        public class BestSellingProduct
        {
            public int ProductId { get; set; }
            public string ProductName { get; set; } = string.Empty;
            public string Category { get; set; } = string.Empty;
            public int TotalQuantitySold { get; set; }
            public decimal TotalRevenue { get; set; }
            public int OrderCount { get; set; }
        }
    }
}
