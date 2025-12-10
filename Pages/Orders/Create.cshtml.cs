using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OrderManagementRazor.Models;

namespace OrderManagementRazor.Pages.Orders
{
    [Authorize]
    public class CreateModel : PageModel
    {
        private readonly AppDbContext _context;

        public CreateModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Order Order { get; set; } = new();

        public SelectList? Customers { get; set; }
        public SelectList? Agents { get; set; }
        public List<Product> Products { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadDropdowns();
            
            // Generate order number
            var lastOrder = await _context.Orders.OrderByDescending(o => o.OrderId).FirstOrDefaultAsync();
            var orderNumber = lastOrder != null ? $"ORD{(lastOrder.OrderId + 1):D6}" : "ORD000001";
            Order.OrderNumber = orderNumber;
            Order.OrderDate = DateTime.Now;
            
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(List<int> productIds, List<int> quantities)
        {
            if (!ModelState.IsValid)
            {
                await LoadDropdowns();
                return Page();
            }

            if (productIds == null || !productIds.Any())
            {
                ModelState.AddModelError(string.Empty, "Please add at least one product to the order.");
                await LoadDropdowns();
                return Page();
            }

            Order.TotalAmount = 0;

            _context.Orders.Add(Order);

            // Add order details
            for (int i = 0; i < productIds.Count; i++)
            {
                if (quantities[i] > 0)
                {
                    var product = await _context.Products.FindAsync(productIds[i]);
                    if (product != null)
                    {
                        var lineTotal = product.UnitPrice * quantities[i];

                        var orderDetail = new OrderDetail
                        {
                            ProductId = productIds[i],
                            Quantity = quantities[i],
                            UnitPrice = product.UnitPrice,
                            LineTotal = lineTotal
                        };

                        Order.OrderDetails.Add(orderDetail);
                        Order.TotalAmount += lineTotal;

                        // Update stock
                        product.StockQuantity -= quantities[i];
                    }
                }
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Order created successfully!";
            return RedirectToPage("./Index");
        }

        private async Task LoadDropdowns()
        {
            Customers = new SelectList(await _context.Customers.ToListAsync(), "CustomerId", "CustomerName");
            Agents = new SelectList(await _context.Agents.ToListAsync(), "AgentId", "AgentName");
            Products = await _context.Products.Where(p => p.StockQuantity > 0).ToListAsync();
        }
    }
}
