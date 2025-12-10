using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OrderManagementRazor.Models;

namespace OrderManagementRazor.Pages.Reports
{
    [Authorize]
    public class CustomerPurchasesModel : PageModel
    {
        private readonly AppDbContext _context;

        public CustomerPurchasesModel(AppDbContext context)
        {
            _context = context;
        }

        public List<CustomerPurchase> CustomerPurchases { get; set; } = new();
        public SelectList? Customers { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? CustomerId { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? StartDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? EndDate { get; set; }

        public async Task OnGetAsync()
        {
            Customers = new SelectList(await _context.Customers.ToListAsync(), "CustomerId", "CustomerName");

            var query = _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .AsQueryable();

            // Apply filters
            if (CustomerId.HasValue)
            {
                query = query.Where(o => o.CustomerId == CustomerId.Value);
            }

            if (StartDate.HasValue)
            {
                query = query.Where(o => o.OrderDate >= StartDate.Value);
            }

            if (EndDate.HasValue)
            {
                query = query.Where(o => o.OrderDate <= EndDate.Value);
            }

            var orders = await query.ToListAsync();

            CustomerPurchases = orders
                .SelectMany(o => o.OrderDetails.Select(od => new
                {
                    o.Customer,
                    od.Product,
                    od.Quantity,
                    od.LineTotal,
                    o.OrderDate,
                    o.OrderNumber
                }))
                .GroupBy(x => new
                {
                    x.Customer!.CustomerId,
                    x.Customer.CustomerName,
                    x.Customer.Email,
                    x.Product!.ProductId,
                    x.Product.ProductName
                })
                .Select(g => new CustomerPurchase
                {
                    CustomerId = g.Key.CustomerId,
                    CustomerName = g.Key.CustomerName,
                    CustomerEmail = g.Key.Email ?? "",
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.ProductName,
                    TotalQuantity = g.Sum(x => x.Quantity),
                    TotalSpent = g.Sum(x => x.LineTotal),
                    PurchaseCount = g.Count(),
                    LastPurchaseDate = g.Max(x => x.OrderDate)
                })
                .OrderBy(cp => cp.CustomerName)
                .ThenByDescending(cp => cp.TotalSpent)
                .ToList();
        }

        public class CustomerPurchase
        {
            public int CustomerId { get; set; }
            public string CustomerName { get; set; } = string.Empty;
            public string CustomerEmail { get; set; } = string.Empty;
            public int ProductId { get; set; }
            public string ProductName { get; set; } = string.Empty;
            public int TotalQuantity { get; set; }
            public decimal TotalSpent { get; set; }
            public int PurchaseCount { get; set; }
            public DateTime LastPurchaseDate { get; set; }
        }
    }
}
