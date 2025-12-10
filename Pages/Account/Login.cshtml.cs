using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrderManagementRazor.Models;
using System.Security.Claims;

namespace OrderManagementRazor.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(AppDbContext context, ILogger<LoginModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public LoginViewModel LoginInput { get; set; } = new();

        public string? ReturnUrl { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public async Task OnGetAsync(string? returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");

            // Clear existing authentication
            try
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error signing out user");
            }

            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                // Validate input is not empty
                if (string.IsNullOrWhiteSpace(LoginInput.Username) || 
                    string.IsNullOrWhiteSpace(LoginInput.Password))
                {
                    ModelState.AddModelError(string.Empty, "Username and password are required.");
                    return Page();
                }

                // Find user in database
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == LoginInput.Username);

                if (user == null)
                {
                    // User not found
                    _logger.LogWarning("Login attempt failed: User '{Username}' not found", LoginInput.Username);
                    ModelState.AddModelError(string.Empty, "Invalid username or password.");
                    return Page();
                }

                // Check password
                if (user.Password != LoginInput.Password)
                {
                    // Incorrect password
                    _logger.LogWarning("Login attempt failed: Incorrect password for user '{Username}'", LoginInput.Username);
                    ModelState.AddModelError(string.Empty, "Invalid username or password.");
                    return Page();
                }

                // Successful authentication - create claims
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                    new Claim(ClaimTypes.GivenName, user.FullName ?? string.Empty),
                    new Claim(ClaimTypes.Email, user.Email ?? string.Empty)
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = LoginInput.RememberMe,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8),
                    AllowRefresh = true
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                _logger.LogInformation("User '{Username}' logged in successfully", user.Username);

                return LocalRedirect(returnUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login process");
                ModelState.AddModelError(string.Empty, "An error occurred during login. Please try again.");
                return Page();
            }
        }
    }
}
