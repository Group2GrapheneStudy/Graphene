using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Graphene_Group_Project.Data;
using Graphene_Group_Project.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Graphene_Group_Project.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        // -------------------- VIEW MODELS --------------------

        public class LoginViewModel
        {
            [Required, EmailAddress]
            public string Email { get; set; } = string.Empty;

            [Required, DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;

            [Required]
            public string Role { get; set; } = string.Empty;
        }

        public class RegisterViewModel
        {
            [Required]
            public string FullName { get; set; } = string.Empty;

            [Required, EmailAddress]
            public string Email { get; set; } = string.Empty;

            [Required, DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;

            [Required, DataType(DataType.Password)]
            [Compare("Password", ErrorMessage = "Passwords do not match.")]
            public string ConfirmPassword { get; set; } = string.Empty;

            [Required]
            public string Role { get; set; } = string.Empty;
        }

        // -------------------- LOGIN --------------------

        [HttpGet]
        public IActionResult Login(string? role)
        {
            // pre-select role if user clicked from a portal tile
            var model = new LoginViewModel
            {
                Role = string.IsNullOrEmpty(role) ? "" : role
            };

            return View(model);
        }

        [HttpPost]
        public IActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = _context.UserAccounts.FirstOrDefault(u =>
                u.Email == model.Email &&
                u.Password == model.Password &&     // NOTE: for coursework only
                u.Role == model.Role);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid email, password or role.");
                return View(model);
            }

            // ✅ save user info in session
            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("UserRole", user.Role);
            HttpContext.Session.SetString("UserName", user.FullName);

            // ✅ redirect to appropriate dashboard
            return RedirectToRoleDashboard(user.Role);
        }

        // -------------------- REGISTER --------------------

        [HttpGet]
        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        [HttpPost]
        public IActionResult Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (_context.UserAccounts.Any(u => u.Email == model.Email))
            {
                ModelState.AddModelError(nameof(model.Email),
                    "An account with this email already exists.");
                return View(model);
            }

            var user = new UserAccount
            {
                FullName = model.FullName,
                Email = model.Email,
                Password = model.Password,  // coursework only
                Role = model.Role
            };

            _context.UserAccounts.Add(user);
            _context.SaveChanges();

            // ✅ immediately log the user in & redirect to their dashboard
            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("UserRole", user.Role);
            HttpContext.Session.SetString("UserName", user.FullName);

            return RedirectToRoleDashboard(user.Role);
        }

        // -------------------- LOGOUT --------------------

        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            TempData["Message"] = "You have been logged out.";
            return RedirectToAction("Index", "Home");
        }

        // -------------------- ACCESS DENIED --------------------

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        // -------------------- HELPER --------------------

        private IActionResult RedirectToRoleDashboard(string role)
        {
            switch (role)
            {
                case "Admin":
                    return RedirectToAction("Admin", "Dashboard");      

                case "Clinician":
                    return RedirectToAction("Clinician", "Dashboard"); 

                case "Patient":
                    return RedirectToAction("Patient", "Dashboard");  

                default:
                    return RedirectToAction("Index", "Home");
            }
        }
    }
}
