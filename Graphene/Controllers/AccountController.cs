using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.AspNetCore.Mvc;

namespace Graphene_Group_Project.Controllers
{
    public class AccountController : Controller
    {
        // Simple in-memory account model just for this demo
        public class UserAccount
        {
            public int Id { get; set; }
            public string FullName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty; // plain text for coursework demo
            public string Role { get; set; } = string.Empty;      // "Patient", "Clinician", "Admin"
        }

        // ViewModels for the Login / Register forms
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

        // --------------------------------------------------------------------
        //  In-memory user store
        // --------------------------------------------------------------------

        private static readonly List<UserAccount> _accounts = new List<UserAccount>();
        private static int _nextId = 1;

        static AccountController()
        {
            // Seed with 1 user per role
            _accounts.Add(new UserAccount
            {
                Id = _nextId++,
                FullName = "Admin User",
                Email = "admin@example.com",
                Password = "Admin123!",   // demo password
                Role = "Admin"
            });

            _accounts.Add(new UserAccount
            {
                Id = _nextId++,
                FullName = "Demo Clinician",
                Email = "clinician@example.com",
                Password = "Clinician123!",
                Role = "Clinician"
            });

            _accounts.Add(new UserAccount
            {
                Id = _nextId++,
                FullName = "Demo Patient",
                Email = "patient@example.com",
                Password = "Patient123!",
                Role = "Patient"
            });
        }

        // --------------------------------------------------------------------
        //  LOGIN
        // --------------------------------------------------------------------

        [HttpGet]
        public IActionResult Login()
        {
            return View(new LoginViewModel());
        }

        [HttpPost]
        public IActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = _accounts.FirstOrDefault(u =>
                u.Email.Equals(model.Email, StringComparison.OrdinalIgnoreCase) &&
                u.Password == model.Password &&
                u.Role.Equals(model.Role, StringComparison.OrdinalIgnoreCase));

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid email, password or role.");
                return View(model);
            }

            // For coursework we simply redirect by role (no cookie/session needed)
            if (user.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("Dashboard", "Admin");
            }
            if (user.Role.Equals("Clinician", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("Dashboard", "Clinician");
            }
            if (user.Role.Equals("Patient", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("Dashboard", "Patient");
            }

            // Fallback – unknown role
            return RedirectToAction("Index", "Home");
        }

        // --------------------------------------------------------------------
        //  REGISTER
        // --------------------------------------------------------------------

        [HttpGet]
        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        [HttpPost]
        public IActionResult Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Check if email already exists
            var existing = _accounts.FirstOrDefault(u =>
                u.Email.Equals(model.Email, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                ModelState.AddModelError(nameof(model.Email), "An account with this email already exists.");
                return View(model);
            }

            var newAccount = new UserAccount
            {
                Id = _nextId++,
                FullName = model.FullName,
                Email = model.Email,
                Password = model.Password,
                Role = model.Role
            };

            _accounts.Add(newAccount);

            TempData["Message"] = "Registration successful! You can now log in.";
            return RedirectToAction("Login");
        }

        // --------------------------------------------------------------------
        //  LOGOUT (dummy for now – no session/cookies used)
        // --------------------------------------------------------------------

        [HttpGet]
        public IActionResult Logout()
        {
            TempData["Message"] = "You have been logged out.";
            return RedirectToAction("Index", "Home");
        }
    }
}
