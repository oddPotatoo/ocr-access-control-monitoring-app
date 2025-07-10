using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using OCR_AccessControl.Models;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Scripting;

namespace OCR_AccessControl.Controllers
{
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UserController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(Users user)
        {
            var dbUser = _context.Users.FirstOrDefault(u => u.Email == user.Email);

            if (dbUser != null && BCrypt.Net.BCrypt.Verify(user.Password, dbUser.Password))
            {
                // Create claims for the authenticated user
                var claims = new List<Claim>
                {
                new Claim(ClaimTypes.Name, dbUser.Email ?? "Unknown"),
                new Claim(ClaimTypes.NameIdentifier, dbUser.Id.ToString())
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                // Sign in the user
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    new AuthenticationProperties
                    {
                        IsPersistent = true // Optional: Keep the user logged in across sessions
                    });

                // Redirect to the dashboard
                return RedirectToAction("Index", "Dashboard");
            }

            ModelState.AddModelError(string.Empty, "Invalid username or password");
            return View("~/Views/Home/Index.cshtml"); // Explicit path
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            // Sign out the user
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }
    }
}