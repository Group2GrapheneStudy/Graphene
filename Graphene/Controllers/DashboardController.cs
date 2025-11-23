using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GrapheneTrace.Web.Controllers
{
    public class DashboardController : Controller
    {
        // ----------------- ROLE HELPERS -----------------
        private string? UserRole => HttpContext.Session.GetString("UserRole");

        private bool IsAdmin => UserRole == "Admin";
        private bool IsClinician => UserRole == "Clinician";
        private bool IsPatient => UserRole == "Patient";

        // ----------------- SIMPLE IN-MEMORY MODELS -----------------
        public class AdminUserSummary
        {
            public int Id { get; set; }
            public string FullName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Role { get; set; } = string.Empty; // "Patient", "Clinician", "Admin"
            public bool IsAvailable { get; set; }            // Mainly for clinicians
        }

        public class AppointmentItem
        {
            public int Id { get; set; }
            public string PatientName { get; set; } = string.Empty;
            public string ClinicianName { get; set; } = string.Empty;
            public DateTime StartTime { get; set; }
            public DateTime EndTime { get; set; }
            public string Status { get; set; } = "Scheduled"; // "Scheduled", "Completed", "Cancelled"
        }

        public class AlertItem
        {
            public int Id { get; set; }
            public string PatientName { get; set; } = string.Empty;
            public string Severity { get; set; } = "Medium"; // "Low", "Medium", "High"
            public string Message { get; set; } = string.Empty;
            public DateTime RaisedAt { get; set; }
            public bool IsResolved { get; set; }
        }

        public class AdminViewModel
        {
            public int TotalUsers { get; set; }
            public int TotalPatients { get; set; }
            public int TotalClinicians { get; set; }
            public int OpenAlerts { get; set; }
            public int UpcomingAppointments { get; set; }

            public List<AdminUserSummary> Patients { get; set; } = new();
            public List<AdminUserSummary> Clinicians { get; set; } = new();
            public List<AppointmentItem> Appointments { get; set; } = new();
            public List<AlertItem> Alerts { get; set; } = new();
        }

        // ----------------- DEMO DATA -----------------
        private static readonly List<AdminUserSummary> _users = new()
        {
            new AdminUserSummary { Id = 1, FullName = "Alice Patient",   Email = "alice@example.com",   Role = "Patient",   IsAvailable = true },
            new AdminUserSummary { Id = 2, FullName = "Bob Patient",     Email = "bob@example.com",     Role = "Patient",   IsAvailable = true },
            new AdminUserSummary { Id = 3, FullName = "Dr. Clark",       Email = "clark@example.com",   Role = "Clinician", IsAvailable = true },
            new AdminUserSummary { Id = 4, FullName = "Dr. Dana",        Email = "dana@example.com",    Role = "Clinician", IsAvailable = false },
            new AdminUserSummary { Id = 5, FullName = "System Admin",    Email = "admin@example.com",   Role = "Admin",     IsAvailable = true }
        };

        private static readonly List<AppointmentItem> _appointments = new()
        {
            new AppointmentItem
            {
                Id = 1,
                PatientName = "Alice Patient",
                ClinicianName = "Dr. Clark",
                StartTime = DateTime.Today.AddHours(10),
                EndTime = DateTime.Today.AddHours(10.5),
                Status = "Scheduled"
            },
            new AppointmentItem
            {
                Id = 2,
                PatientName = "Bob Patient",
                ClinicianName = "Dr. Dana",
                StartTime = DateTime.Today.AddHours(14),
                EndTime = DateTime.Today.AddHours(14.5),
                Status = "Scheduled"
            }
        };

        private static readonly List<AlertItem> _alerts = new()
        {
            new AlertItem
            {
                Id = 1,
                PatientName = "Alice Patient",
                Severity = "High",
                Message = "Sustained high pressure on left hip.",
                RaisedAt = DateTime.Now.AddMinutes(-35),
                IsResolved = false
            },
            new AlertItem
            {
                Id = 2,
                PatientName = "Bob Patient",
                Severity = "Medium",
                Message = "No movement detected for 45 minutes.",
                RaisedAt = DateTime.Now.AddHours(-1),
                IsResolved = false
            },
            new AlertItem
            {
                Id = 3,
                PatientName = "Alice Patient",
                Severity = "Low",
                Message = "Short peak pressure observed.",
                RaisedAt = DateTime.Now.AddHours(-4),
                IsResolved = true
            }
        };

        private static int _nextUserId = _users.Max(u => u.Id) + 1;
        private static int _nextAppointmentId = _appointments.Max(a => a.Id) + 1;
        private static int _nextAlertId = _alerts.Max(a => a.Id) + 1;

        // ----------------- PATIENT DASHBOARD -----------------
        public IActionResult Patient()
        {
            if (!(IsPatient || IsClinician || IsAdmin))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            ViewData["Title"] = "Patient Dashboard";
            return View();
        }

        // ----------------- CLINICIAN DASHBOARD -----------------
        public IActionResult Clinician()
        {
            if (!(IsClinician || IsAdmin))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            ViewData["Title"] = "Clinician Dashboard";
            return View();
        }

        // ----------------- ADMIN DASHBOARD (MAIN PANEL) -----------------
        [HttpGet]
        public IActionResult Admin()
        {
            if (!IsAdmin)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var patients = _users.Where(u => u.Role == "Patient").ToList();
            var clinicians = _users.Where(u => u.Role == "Clinician").ToList();
            var openAlerts = _alerts.Where(a => !a.IsResolved).OrderByDescending(a => a.RaisedAt).ToList();
            var upcomingAppointments = _appointments
                .Where(a => a.StartTime >= DateTime.Today)
                .OrderBy(a => a.StartTime)
                .ToList();

            var vm = new AdminViewModel
            {
                TotalUsers = _users.Count,
                TotalPatients = patients.Count,
                TotalClinicians = clinicians.Count,
                OpenAlerts = openAlerts.Count,
                UpcomingAppointments = upcomingAppointments.Count,
                Patients = patients,
                Clinicians = clinicians,
                Appointments = upcomingAppointments,
                Alerts = openAlerts
            };

            ViewData["Title"] = "Admin Dashboard";
            return View(vm);
        }

        // ----------------- ACTIONS: CREATE USER (GENERIC) -----------------
        [HttpPost]
        public IActionResult CreateUser(string fullName, string email, string role)
        {
            if (!IsAdmin)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            if (string.IsNullOrWhiteSpace(fullName) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(role))
            {
                TempData["AdminMessage"] = "Please fill in all user details.";
                return RedirectToAction("Admin");
            }

            _users.Add(new AdminUserSummary
            {
                Id = _nextUserId++,
                FullName = fullName.Trim(),
                Email = email.Trim(),
                Role = role.Trim(),
                IsAvailable = role == "Clinician"
            });

            TempData["AdminMessage"] = $"User '{fullName}' created as {role}.";
            return RedirectToAction("Admin");
        }

        // ----------------- ACTION: QUICK ADD CLINICIAN -----------------
        [HttpPost]
        public IActionResult CreateClinician(string fullName, string email)
        {
            if (!IsAdmin)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email))
            {
                TempData["AdminMessage"] = "Clinician name and email are required.";
                return RedirectToAction("Admin");
            }

            _users.Add(new AdminUserSummary
            {
                Id = _nextUserId++,
                FullName = fullName.Trim(),
                Email = email.Trim(),
                Role = "Clinician",
                IsAvailable = true
            });

            TempData["AdminMessage"] = $"Clinician '{fullName}' added to the team.";
            return RedirectToAction("Admin");
        }

        // ----------------- ACTION: DELETE PATIENT -----------------
        [HttpPost]
        public IActionResult DeletePatient(int id)
        {
            if (!IsAdmin)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var patient = _users.FirstOrDefault(u => u.Id == id && u.Role == "Patient");
            if (patient != null)
            {
                _users.Remove(patient);

                // Clean up related appointments and alerts
                _appointments.RemoveAll(a => a.PatientName == patient.FullName);
                _alerts.RemoveAll(a => a.PatientName == patient.FullName);

                TempData["AdminMessage"] = $"Patient '{patient.FullName}' has been removed from the system.";
            }

            return RedirectToAction("Admin");
        }

        // ----------------- ACTIONS: SCHEDULE APPOINTMENT -----------------
        [HttpPost]
        public IActionResult ScheduleAppointment(string patientName, string clinicianName, DateTime startTime, DateTime endTime)
        {
            if (!IsAdmin)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            if (string.IsNullOrWhiteSpace(patientName) ||
                string.IsNullOrWhiteSpace(clinicianName) ||
                startTime == default ||
                endTime == default ||
                endTime <= startTime)
            {
                TempData["AdminMessage"] = "Please provide valid appointment details.";
                return RedirectToAction("Admin");
            }

            _appointments.Add(new AppointmentItem
            {
                Id = _nextAppointmentId++,
                PatientName = patientName.Trim(),
                ClinicianName = clinicianName.Trim(),
                StartTime = startTime,
                EndTime = endTime,
                Status = "Scheduled"
            });

            TempData["AdminMessage"] = $"Appointment scheduled for {patientName} with {clinicianName}.";
            return RedirectToAction("Admin");
        }

        // ----------------- ACTIONS: TOGGLE CLINICIAN AVAILABILITY -----------------
        [HttpPost]
        public IActionResult ToggleClinicianAvailability(int id)
        {
            if (!IsAdmin)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var clinician = _users.FirstOrDefault(u => u.Id == id && u.Role == "Clinician");
            if (clinician != null)
            {
                clinician.IsAvailable = !clinician.IsAvailable;
                TempData["AdminMessage"] = $"{clinician.FullName} is now " +
                                           (clinician.IsAvailable ? "available" : "unavailable") + ".";
            }

            return RedirectToAction("Admin");
        }

        // ----------------- ACTIONS: RESOLVE ALERT -----------------
        [HttpPost]
        public IActionResult ResolveAlert(int id)
        {
            if (!IsAdmin)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var alert = _alerts.FirstOrDefault(a => a.Id == id);
            if (alert != null)
            {
                alert.IsResolved = true;
                TempData["AdminMessage"] = $"Alert for {alert.PatientName} has been marked as resolved.";
            }

            return RedirectToAction("Admin");
        }

        // ----------------- ACTION: CREATE ALERT -----------------
        [HttpPost]
        public IActionResult CreateAlert(string patientName, string severity, string message)
        {
            if (!IsAdmin)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            if (string.IsNullOrWhiteSpace(patientName) ||
                string.IsNullOrWhiteSpace(severity) ||
                string.IsNullOrWhiteSpace(message))
            {
                TempData["AdminMessage"] = "Please fill in all alert details.";
                return RedirectToAction("Admin");
            }

            _alerts.Add(new AlertItem
            {
                Id = _nextAlertId++,
                PatientName = patientName.Trim(),
                Severity = severity.Trim(),
                Message = message.Trim(),
                RaisedAt = DateTime.Now,
                IsResolved = false
            });

            TempData["AdminMessage"] = $"Alert created for {patientName} ({severity}).";
            return RedirectToAction("Admin");
        }
    }
}
