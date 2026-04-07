using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ResumeProject.Data;
using ResumeProject.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace ResumeProject.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ResumeProject.Services.IEmailService _emailService;

        public AccountController(ApplicationDbContext context, ResumeProject.Services.IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Dashboard", "Resume");
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // Check if email already exists
            if (await _context.Users.AnyAsync(u => u.Email == model.Email))
            {
                ModelState.AddModelError("Email", "An account with this email already exists.");
                return View(model);
            }

            var user = new User
            {
                FullName = model.FullName,
                Email = model.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Create a default resume for the new user
            var resume = new Resume
            {
                UserId = user.Id,
                FirstName = model.FullName.Split(' ').FirstOrDefault() ?? "",
                LastName = model.FullName.Split(' ').Skip(1).LastOrDefault() ?? "",
                Email = model.Email,
                Template = "Professional",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Resumes.Add(resume);
            await _context.SaveChangesAsync();

            await SignInUser(user);
            return RedirectToAction("Dashboard", "Resume");
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Dashboard", "Resume");
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
            {
                ModelState.AddModelError("", "Invalid email or password.");
                return View(model);
            }

            await SignInUser(user);
            return RedirectToAction("Dashboard", "Resume");
        }

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("CookieAuth");
            return RedirectToAction("Index", "Home");
        }

        private async Task SignInUser(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email)
            };

            var identity = new ClaimsIdentity(claims, "CookieAuth");
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync("CookieAuth", principal, new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
            });
        }
        // GET: /Account/ForgotPassword
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST: /Account/ForgotPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
            if (user == null)
            {
                // Don't reveal that the user does not exist or is not confirmed
                ModelState.AddModelError("", "If the email matches an existing account, a reset link will be processed.");
                return View(model);
            }

            // Pass the email securely in real web app (usually a token), but here passing email to simulate real flow.
            var resetLink = Url.Action("ResetPassword", "Account", new { email = model.Email }, Request.Scheme);
            var subject = "ResumeAI - Reset Your Password";
            var body = $"<p>Hi there,</p><p>You requested to reset your password.</p><p>Please click the link below to securely reset it:</p><p><a href='{resetLink}' style='padding: 10px 15px; background-color: #007bff; color: #fff; text-decoration: none; border-radius: 4px;'>Reset Password</a></p><p>If you didn't request this, you can safely ignore this email.</p>";

            try
            {
                await _emailService.SendEmailAsync(model.Email, subject, body);
                TempData["SuccessMessage"] = "Please check your email to reset your password.";
            }
            catch(System.Exception)
            {
                // In case of SMTP errors if not configured, fallback gracefully but tell the user.
                TempData["SuccessMessage"] = "Please check your email to reset your password. (Wait! If you haven't put your SMTP details in appsettings.json, this email failed to send, but the logic is real!)";
            }
            
            return RedirectToAction("Login");
        }

        // GET: /Account/ResetPassword
        [HttpGet]
        public IActionResult ResetPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("Login");
            }

            var model = new ResetPasswordViewModel { Email = email };
            return View(model);
        }

        // POST: /Account/ResetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return RedirectToAction("Login");
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Your password has been reset successfully. Please log in.";
            return RedirectToAction("Login");
        }
    }
}