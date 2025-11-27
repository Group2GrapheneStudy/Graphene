using System.ComponentModel.DataAnnotations;
using System.Linq;
using Graphene_Group_Project.Data;
using Graphene_Group_Project.Models;
using GrapheneTrace.Web.Controllers;   // for DashboardController.RegisterUserFromAccount
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

        // -------- VIEW MODELS --------

        public class LoginViewModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; } = string.Empty;

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;

            [Required]
            public string Role { get; set; } = string.Empty;
        }

        public class RegisterViewModel
        {
            [Required]
            [Display(Name = "Full name")]
            public string FullName { get; set; } = string.Empty;

            [Required]
            [EmailAddress]
            public string Email { get; set; } = string.Empty;

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;

            [Required]
            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "Passwords do not match.")]
            public string ConfirmPassword { get; set; } = string.Empty;

            [Required]
            public string Role { get; set; } = "Patient";

            // for patients only
            [Display(Name = "Preferred clinician")]
            public string? SelectedClinician { get; set; }
        }

        // -------- LOGIN --------

        [HttpGet]
        public IActionResult Login()
        {
            return View(new LoginViewModel());
        }

        [HttpPost]
        public IActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = _context.UserAccounts.FirstOrDefault(u =>
                u.Email == model.Email &&
                u.Password == model.Password &&
                u.Role == model.Role);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid credentials or role.");
                return View(model);
            }

            HttpContext.Session.SetString("UserEmail", user.Email);
            HttpContext.Session.SetString("UserRole", user.Role);
            HttpContext.Session.SetString("UserName", user.FullName ?? user.Email);

            return user.Role switch
            {
                "Admin" => RedirectToAction("Admin", "Dashboard"),
                "Clinician" => RedirectToAction("Clinician", "Dashboard"),
                _ => RedirectToAction("Patient", "Dashboard")
            };
        }

        // -------- REGISTER --------

        private void PopulateClinicianDropdown()
        {
            ViewBag.Clinicians = DashboardController.GetClinicianNames();
        }

        [HttpGet]
        public IActionResult Register()
        {
            PopulateClinicianDropdown();
            return View(new RegisterViewModel());
        }

        [HttpPost]
        public IActionResult Register(RegisterViewModel model)
        {
            PopulateClinicianDropdown();

            if (!ModelState.IsValid)
                return View(model);

            if (_context.UserAccounts.Any(u => u.Email == model.Email))
            {
                ModelState.AddModelError(nameof(model.Email), "An account with this email already exists.");
                return View(model);
            }

            if (model.Role == "Patient" && string.IsNullOrWhiteSpace(model.SelectedClinician))
            {
                ModelState.AddModelError(nameof(model.SelectedClinician), "Please choose your clinician.");
                return View(model);
            }

            var user = new UserAccount
            {
                FullName = model.FullName.Trim(),
                Email = model.Email.Trim(),
                Password = model.Password, // coursework only
                Role = model.Role.Trim()
            };

            _context.UserAccounts.Add(user);
            _context.SaveChanges();

            var clinicianName = model.Role == "Patient" ? model.SelectedClinician : null;

            DashboardController.RegisterUserFromAccount(
                model.FullName,
                model.Email,
                model.Role,
                clinicianName);

            HttpContext.Session.SetString("UserEmail", user.Email);
            HttpContext.Session.SetString("UserRole", user.Role);
            HttpContext.Session.SetString("UserName", user.FullName ?? user.Email);

            return user.Role switch
            {
                "Admin" => RedirectToAction("Admin", "Dashboard"),
                "Clinician" => RedirectToAction("Clinician", "Dashboard"),
                _ => RedirectToAction("Patient", "Dashboard")
            };
        }

        // -------- LOGOUT & ACCESS DENIED --------

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
