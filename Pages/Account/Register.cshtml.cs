using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrderManagementRazor.Models;
using System.Security.Claims;

namespace OrderManagementRazor.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly ILogger<RegisterModel> _logger;

        public RegisterModel(AppDbContext context, ILogger<RegisterModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public RegisterViewModel RegisterInput { get; set; } = new();

        public string? ReturnUrl { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public void OnGet(string? returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            ReturnUrl = returnUrl ?? Url.Content("~/");
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
                // Trim and validate inputs
                RegisterInput.Username = RegisterInput.Username?.Trim() ?? string.Empty;
                RegisterInput.Email = RegisterInput.Email?.Trim() ?? string.Empty;
                RegisterInput.FullName = RegisterInput.FullName?.Trim() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(RegisterInput.Username))
                {
                    ModelState.AddModelError(string.Empty, "Username cannot be empty.");
                    return Page();
                }

                // Check if username already exists (case-insensitive)
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username.ToLower() == RegisterInput.Username.ToLower());

                if (existingUser != null)
                {
                    _logger.LogWarning("Registration failed: Username '{Username}' already exists", RegisterInput.Username);
                    ModelState.AddModelError("RegisterInput.Username", "This username is already taken. Please choose a different username.");
                    return Page();
                }

                // Check if email already exists (case-insensitive)
                var existingEmail = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == RegisterInput.Email.ToLower());

                if (existingEmail != null)
                {
                    _logger.LogWarning("Registration failed: Email '{Email}' already registered", RegisterInput.Email);
                    ModelState.AddModelError("RegisterInput.Email", "This email is already registered. Please use a different email or login.");
                    return Page();
                }

                // Create new user
                // SECURITY NOTE: In production, you should hash passwords using a proper algorithm
                // such as BCrypt, PBKDF2, or ASP.NET Core Identity's password hasher.
                // Example: using BCrypt.Net-Next NuGet package:
                // string passwordHash = BCrypt.Net.BCrypt.HashPassword(RegisterInput.Password);
                var newUser = new User
                {
                    Username = RegisterInput.Username,
                    FullName = RegisterInput.FullName,
                    Email = RegisterInput.Email,
                    Password = RegisterInput.Password, // TODO: Hash password in production!
                    CreatedDate = DateTime.Now
                };

                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                _logger.LogInformation("New user registered: '{Username}'", newUser.Username);

                // Automatically log in the user after registration
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, newUser.Username),
                    new Claim(ClaimTypes.NameIdentifier, newUser.UserId.ToString()),
                    new Claim(ClaimTypes.GivenName, newUser.FullName),
                    new Claim(ClaimTypes.Email, newUser.Email)
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = false,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8),
                    AllowRefresh = true
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                _logger.LogInformation("User '{Username}' automatically logged in after registration", newUser.Username);

                TempData["SuccessMessage"] = $"Welcome, {newUser.FullName}! Your account has been created successfully.";
                return LocalRedirect(returnUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user registration");
                ModelState.AddModelError(string.Empty, "An error occurred during registration. Please try again.");
                return Page();
            }
        }
    }
}
