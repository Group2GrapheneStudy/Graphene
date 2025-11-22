using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;

namespace Graphene_Group_Project.Controllers
{
    public class AdminController : Controller
    {
        // --------------------------------------------------------------------
        //  Simple in-memory models so the controller works even without EF.
        //  Later you can replace these with your real database entities.
        // --------------------------------------------------------------------

        private class UserSummary
        {
            public int UserId { get; set; }
            public string FullName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Role { get; set; } = string.Empty;          // "Patient", "Clinician", "Admin"
            public string AccountStatus { get; set; } = "Active";     // "Active" / "Disabled"
        }

        private class AlertSummary
        {
            public int AlertId { get; set; }
            public string PatientName { get; set; } = string.Empty;
            public string Severity { get; set; } = "Medium";          // "Low", "Medium", "High"
            public bool IsReviewed { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        // Demo in-memory data
        private static readonly List<UserSummary> _users = new List<UserSummary>
        {
            new UserSummary { UserId = 1, FullName = "Alice Patient",   Email = "alice@example.com",   Role = "Patient",   AccountStatus = "Active"   },
            new UserSummary { UserId = 2, FullName = "Bob Clinician",   Email = "bob@example.com",     Role = "Clinician", AccountStatus = "Active"   },
            new UserSummary { UserId = 3, FullName = "Charlie Admin",   Email = "admin@example.com",   Role = "Admin",     AccountStatus = "Active"   },
            new UserSummary { UserId = 4, FullName = "Dana Patient",    Email = "dana@example.com",    Role = "Patient",   AccountStatus = "Disabled" }
        };

        private static readonly List<AlertSummary> _alerts = new List<AlertSummary>
        {
            new AlertSummary { AlertId = 1, PatientName = "Alice Patient", Severity = "High",   IsReviewed = false, CreatedAt = DateTime.Now.AddMinutes(-30) },
            new AlertSummary { AlertId = 2, PatientName = "Dana Patient",  Severity = "Medium", IsReviewed = true,  CreatedAt = DateTime.Now.AddHours(-2)  }
        };

        // --------------------------------------------------------------------
        //  DASHBOARD
        // --------------------------------------------------------------------

        // GET: /Admin or /Admin/Dashboard
        public IActionResult Dashboard()
        {
            // Always get a non-null list for the view
            var users = _users.OrderBy(u => u.FullName).ToList();

            // Top-level counts
            ViewBag.TotalUsers = users.Count;
            ViewBag.PatientCount = users.Count(u => u.Role.Equals("Patient", StringComparison.OrdinalIgnoreCase));
            ViewBag.ClinicianCount = users.Count(u => u.Role.Equals("Clinician", StringComparison.OrdinalIgnoreCase));
            ViewBag.AdminCount = users.Count(u => u.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase));

            // User list for the table – NEVER null
            ViewBag.Users = users;

            // System health placeholders (you can wire these to real values later)
            ViewBag.LastMigration = "Not configured";
            ViewBag.StorageUsage = "Unknown";

            // Alert summary
            ViewBag.ActiveAlerts = _alerts.Count(a => !a.IsReviewed);
            ViewBag.ReviewedAlerts = _alerts.Count(a => a.IsReviewed);

            // Optional flash message from other actions
            ViewBag.Message = TempData["Message"];

            return View();
        }

        // --------------------------------------------------------------------
        //  USER MANAGEMENT STUBS
        //  (These keep your links working without needing extra views yet)
        // --------------------------------------------------------------------

        // GET: /Admin/CreateUser
        [HttpGet]
        public IActionResult CreateUser()
        {
            return Content("CreateUser placeholder – replace with a real create-user form later.");
        }

        // POST: /Admin/CreateUser
        [HttpPost]
        public IActionResult CreateUser(string fullName, string email, string role)
        {
            var nextId = _users.Any() ? _users.Max(u => u.UserId) + 1 : 1;

            _users.Add(new UserSummary
            {
                UserId = nextId,
                FullName = fullName ?? "New User",
                Email = email ?? "new@example.com",
                Role = role ?? "Patient",
                AccountStatus = "Active"
            });

            TempData["Message"] = "User created (demo in-memory data only).";
            return RedirectToAction(nameof(Dashboard));
        }

        // GET: /Admin/EditUser/3
        [HttpGet]
        public IActionResult EditUser(int id)
        {
            var user = _users.FirstOrDefault(u => u.UserId == id);
            if (user == null)
            {
                return NotFound($"User with ID {id} not found.");
            }

            return Content($"EditUser placeholder – you would edit user '{user.FullName}' (ID {id}) here.");
        }

        // POST: /Admin/EditUser
        [HttpPost]
        public IActionResult EditUser(int id, string fullName, string email, string role, string status)
        {
            var user = _users.FirstOrDefault(u => u.UserId == id);
            if (user == null)
            {
                return NotFound($"User with ID {id} not found.");
            }

            user.FullName = string.IsNullOrWhiteSpace(fullName) ? user.FullName : fullName;
            user.Email = string.IsNullOrWhiteSpace(email) ? user.Email : email;
            user.Role = string.IsNullOrWhiteSpace(role) ? user.Role : role;
            user.AccountStatus = string.IsNullOrWhiteSpace(status) ? user.AccountStatus : status;

            TempData["Message"] = "User details updated (demo in-memory data only).";
            return RedirectToAction(nameof(Dashboard));
        }

        // GET: /Admin/ToggleStatus/3
        public IActionResult ToggleStatus(int id)
        {
            var user = _users.FirstOrDefault(u => u.UserId == id);
            if (user == null)
            {
                return NotFound($"User with ID {id} not found.");
            }

            user.AccountStatus = user.AccountStatus == "Active" ? "Disabled" : "Active";

            TempData["Message"] = $"User '{user.FullName}' status toggled to {user.AccountStatus} (demo in-memory data).";
            return RedirectToAction(nameof(Dashboard));
        }

        // --------------------------------------------------------------------
        //  SYSTEM TOOLS STUBS
        // --------------------------------------------------------------------

        // GET: /Admin/HealthCheck
        public IActionResult HealthCheck()
        {
            var message =
                "Health Check OK\n" +
                "- Database: simulated\n" +
                "- Alerts: " + _alerts.Count + "\n" +
                "- Users: " + _users.Count;

            return Content(message, "text/plain");
        }

        // GET: /Admin/ViewLogs
        public IActionResult ViewLogs()
        {
            return Content("Log viewer placeholder – no logs are being recorded in this demo.");
        }

        // GET: /Admin/AllAlerts
        public IActionResult AllAlerts()
        {
            var lines = _alerts
                .OrderByDescending(a => a.CreatedAt)
                .Select(a =>
                    $"#{a.AlertId} | Patient: {a.PatientName} | Severity: {a.Severity} | Reviewed: {a.IsReviewed} | Time: {a.CreatedAt:g}");

            var output = "System Alerts:\n" + string.Join("\n", lines);
            return Content(output, "text/plain");
        }

        // GET: /Admin/PurgeData
        public IActionResult PurgeData()
        {
            _alerts.Clear();
            TempData["Message"] = "Demo alerts cleared (in-memory only).";
            return RedirectToAction(nameof(Dashboard));
        }
    }
}
