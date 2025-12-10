using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OrderManagementRazor.Models;

namespace OrderManagementRazor.Pages.Reports
{
    [Authorize]
    public class ProductCustomersModel : PageModel
    {
        private readonly AppDbContext _context;

        public ProductCustomersModel(AppDbContext context)
        {
            _context = context;
        }

        public List<ProductCustomer> ProductCustomers { get; set; } = new();
        public SelectList? Products { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? ProductId { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? StartDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? EndDate { get; set; }

        public async Task OnGetAsync()
        {
            Products = new SelectList(await _context.Products.ToListAsync(), "ProductId", "ProductName");

            var query = _context.OrderDetails
                .Include(od => od.Product)
                .Include(od => od.Order)
                    .ThenInclude(o => o!.Customer)
                .AsQueryable();

            // Apply filters
            if (ProductId.HasValue)
            {
                query = query.Where(od => od.ProductId == ProductId.Value);
            }

            if (StartDate.HasValue)
            {
                query = query.Where(od => od.Order!.OrderDate >= StartDate.Value);
            }

            if (EndDate.HasValue)
            {
                query = query.Where(od => od.Order!.OrderDate <= EndDate.Value);
            }

            ProductCustomers = await query
                .GroupBy(od => new
                {
                    od.Product!.ProductId,
                    od.Product.ProductName,
                    od.Product.Category,
                    od.Order!.Customer!.CustomerId,
                    od.Order.Customer.CustomerName,
                    od.Order.Customer.Email
                })
                .Select(g => new ProductCustomer
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.ProductName,
                    Category = g.Key.Category ?? "N/A",
                    CustomerId = g.Key.CustomerId,
                    CustomerName = g.Key.CustomerName,
                    CustomerEmail = g.Key.Email ?? "",
                    TotalQuantity = g.Sum(od => od.Quantity),
                    TotalSpent = g.Sum(od => od.LineTotal),
                    OrderCount = g.Count(),
                    FirstPurchase = g.Min(od => od.Order!.OrderDate),
                    LastPurchase = g.Max(od => od.Order!.OrderDate)
                })
                .OrderBy(pc => pc.ProductName)
                .ThenByDescending(pc => pc.TotalQuantity)
                .ToListAsync();
        }

        public class ProductCustomer
        {
            public int ProductId { get; set; }
            public string ProductName { get; set; } = string.Empty;
            public string Category { get; set; } = string.Empty;
            public int CustomerId { get; set; }
            public string CustomerName { get; set; } = string.Empty;
            public string CustomerEmail { get; set; } = string.Empty;
            public int TotalQuantity { get; set; }
            public decimal TotalSpent { get; set; }
            public int OrderCount { get; set; }
            public DateTime FirstPurchase { get; set; }
            public DateTime LastPurchase { get; set; }
        }
    }
}
