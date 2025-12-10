using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrderManagementRazor.Models;

namespace OrderManagementRazor.Pages.Agents
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
        public Agent Agent { get; set; } = new();

        public IActionResult OnGet()
        {
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            Agent.CreatedDate = DateTime.Now;

            _context.Agents.Add(Agent);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Agent created successfully!";
            return RedirectToPage("./Index");
        }
    }
}
