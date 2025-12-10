using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrderManagementRazor.Models;

namespace OrderManagementRazor.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly AppDbContext _context;

        public IndexModel(ILogger<IndexModel> logger, AppDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public int TotalProducts { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalAgents { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public List<Order> RecentOrders { get; set; } = new();

        public async Task OnGetAsync()
        {
            if (User.Identity?.IsAuthenticated ?? false)
            {
                TotalProducts = await _context.Products.CountAsync();
                TotalCustomers = await _context.Customers.CountAsync();
                TotalAgents = await _context.Agents.CountAsync();
                TotalOrders = await _context.Orders.CountAsync();
                TotalRevenue = await _context.Orders.SumAsync(o => o.TotalAmount);

                RecentOrders = await _context.Orders
                    .Include(o => o.Customer)
                    .Include(o => o.Agent)
                    .OrderByDescending(o => o.OrderDate)
                    .Take(5)
                    .ToListAsync();
            }
        }
    }
}
