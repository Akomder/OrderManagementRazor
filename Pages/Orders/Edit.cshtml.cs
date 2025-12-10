using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OrderManagementRazor.Models;

namespace OrderManagementRazor.Pages.Orders
{
    [Authorize]
    public class EditModel : PageModel
    {
        private readonly AppDbContext _context;

        public EditModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Order Order { get; set; } = new();

        public SelectList? Customers { get; set; }
        public SelectList? Agents { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(m => m.OrderId == id);

            if (order == null) return NotFound();

            Order = order;
            await LoadDropdowns();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadDropdowns();
                return Page();
            }

            _context.Attach(Order).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Orders.Any(e => e.OrderId == Order.OrderId))
                    return NotFound();
                else
                    throw;
            }

            TempData["SuccessMessage"] = "Order updated successfully!";
            return RedirectToPage("./Index");
        }

        private async Task LoadDropdowns()
        {
            Customers = new SelectList(await _context.Customers.ToListAsync(), "CustomerId", "CustomerName");
            Agents = new SelectList(await _context.Agents.ToListAsync(), "AgentId", "AgentName");
        }
    }
}
