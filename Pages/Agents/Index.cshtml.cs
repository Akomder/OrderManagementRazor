using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrderManagementRazor.Models;

namespace OrderManagementRazor.Pages.Agents
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;

        public IndexModel(AppDbContext context)
        {
            _context = context;
        }

        public IList<Agent> Agents { get; set; } = new List<Agent>();

        public async Task OnGetAsync()
        {
            Agents = await _context.Agents
                .OrderBy(a => a.AgentName)
                .ToListAsync();
        }
    }
}
