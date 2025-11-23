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

        // ----------------- IN-MEMORY MODELS -----------------
        public class AdminUserSummary
        {
            public int Id { get; set; }
            public string FullName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Role { get; set; } = string.Empty; // "Patient", "Clinician", "Admin"
            public bool IsAvailable { get; set; }            // for clinicians
            public bool IsActive { get; set; } = true;       // soft deactivation
        }

        public class AppointmentItem
        {
            public int Id { get; set; }
            public string PatientName { get; set; } = string.Empty;
            public string ClinicianName { get; set; } = string.Empty;
            public DateTime StartTime { get; set; }
            public DateTime EndTime { get; set; }
            public string Status { get; set; } = "Scheduled";
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

        public class ClinicianWorkloadInfo
        {
            public string Name { get; set; } = string.Empty;
            public int UpcomingAppointments { get; set; }
            public string LoadLabel { get; set; } = "Light"; // Light, Normal, Busy
        }

        public class PatientAlertSummary
        {
            public string PatientName { get; set; } = string.Empty;
            public int OpenAlertCount { get; set; }
        }

        public class AdminViewModel
        {
            // High-level stats
            public int TotalUsers { get; set; }
            public int TotalPatients { get; set; }
            public int TotalClinicians { get; set; }
            public int OpenAlerts { get; set; }
            public int UpcomingAppointments { get; set; }

            // Alert analytics
            public int AlertsToday { get; set; }
            public int HighAlerts { get; set; }
            public int MediumAlerts { get; set; }
            public int LowAlerts { get; set; }
            public List<PatientAlertSummary> TopPatientsByAlerts { get; set; } = new();

            // Core lists
            public List<AdminUserSummary> Patients { get; set; } = new();
            public List<AdminUserSummary> Clinicians { get; set; } = new();
            public List<AppointmentItem> Appointments { get; set; } = new();
            public List<AlertItem> Alerts { get; set; } = new();
            public List<ClinicianWorkloadInfo> ClinicianWorkload { get; set; } = new();

            // Filters
            public string? Search { get; set; }
            public string? RoleFilter { get; set; }
            public bool ShowInactive { get; set; }

            // Settings
            public int HighPressureThreshold { get; set; }
            public int NoMovementMinutes { get; set; }

            // Audit log (last few admin actions)
            public IEnumerable<string> AdminLog { get; set; } = Enumerable.Empty<string>();
        }

        // ----------------- DEMO DATA -----------------
        private static readonly List<AdminUserSummary> _users = new()
        {
            new AdminUserSummary { Id = 1, FullName = "Alice Patient",   Email = "alice@example.com",   Role = "Patient",   IsAvailable = true,  IsActive = true },
            new AdminUserSummary { Id = 2, FullName = "Bob Patient",     Email = "bob@example.com",     Role = "Patient",   IsAvailable = true,  IsActive = true },
            new AdminUserSummary { Id = 3, FullName = "Dr. Clark",       Email = "clark@example.com",   Role = "Clinician", IsAvailable = true,  IsActive = true },
            new AdminUserSummary { Id = 4, FullName = "Dr. Dana",        Email = "dana@example.com",    Role = "Clinician", IsAvailable = false, IsActive = true },
            new AdminUserSummary { Id = 5, FullName = "System Admin",    Email = "admin@example.com",   Role = "Admin",     IsAvailable = true,  IsActive = true }
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

        private static int _nextUserId = _users.Count == 0 ? 1 : _users.Max(u => u.Id) + 1;
        private static int _nextAppointmentId = _appointments.Count == 0 ? 1 : _appointments.Max(a => a.Id) + 1;
        private static int _nextAlertId = _alerts.Count == 0 ? 1 : _alerts.Max(a => a.Id) + 1;

        // System settings (configurable on admin screen)
        private static int _highPressureThreshold = 80;
        private static int _noMovementMinutes = 45;

        // Simple audit log
        private static readonly List<string> _adminLog = new();

        private void LogAdmin(string message)
        {
            var entry = $"{DateTime.Now:dd MMM HH:mm} - {message}";
            _adminLog.Add(entry);
            // Keep last 50 entries only
            if (_adminLog.Count > 50)
            {
                _adminLog.RemoveAt(0);
            }
        }

        // ----------------- PATIENT & CLINICIAN DASHBOARDS -----------------
        public IActionResult Patient()
        {
            if (!(IsPatient || IsClinician || IsAdmin))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            ViewData["Title"] = "Patient Dashboard";
            return View();
        }

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
        public IActionResult Admin(string? search, string? roleFilter, bool showInactive = false)
        {
            if (!IsAdmin)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            // base user query: optionally hide inactive
            IEnumerable<AdminUserSummary> userQuery = _users;
            if (!showInactive)
            {
                userQuery = userQuery.Where(u => u.IsActive);
            }

            // role filter
            if (!string.IsNullOrEmpty(roleFilter) && roleFilter != "All")
            {
                userQuery = userQuery.Where(u =>
                    u.Role.Equals(roleFilter, StringComparison.OrdinalIgnoreCase));
            }

            // search
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                userQuery = userQuery.Where(u =>
                    u.FullName.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                    u.Email.Contains(s, StringComparison.OrdinalIgnoreCase));
            }

            var usersFiltered = userQuery.ToList();
            var patients = usersFiltered.Where(u => u.Role == "Patient").ToList();
            var clinicians = usersFiltered.Where(u => u.Role == "Clinician").ToList();

            var today = DateTime.Today;
            var openAlerts = _alerts.Where(a => !a.IsResolved).OrderByDescending(a => a.RaisedAt).ToList();
            var upcomingAppointments = _appointments
                .Where(a => a.StartTime >= today)
                .OrderBy(a => a.StartTime)
                .ToList();

            // Alert analytics
            var alertsToday = openAlerts.Count(a => a.RaisedAt.Date == today);
            var highAlerts = openAlerts.Count(a => a.Severity == "High");
            var mediumAlerts = openAlerts.Count(a => a.Severity == "Medium");
            var lowAlerts = openAlerts.Count(a => a.Severity == "Low");

            var topPatients = openAlerts
                .GroupBy(a => a.PatientName)
                .Select(g => new PatientAlertSummary
                {
                    PatientName = g.Key,
                    OpenAlertCount = g.Count()
                })
                .OrderByDescending(p => p.OpenAlertCount)
                .Take(3)
                .ToList();

            // Clinician workload
            var clinicianWorkload = clinicians
                .Select(c =>
                {
                    var count = upcomingAppointments.Count(a => a.ClinicianName == c.FullName);
                    var label = count <= 1 ? "Light" : count <= 3 ? "Normal" : "Busy";
                    return new ClinicianWorkloadInfo
                    {
                        Name = c.FullName,
                        UpcomingAppointments = count,
                        LoadLabel = label
                    };
                })
                .ToList();

            var vm = new AdminViewModel
            {
                TotalUsers = _users.Count,
                TotalPatients = _users.Count(u => u.Role == "Patient"),
                TotalClinicians = _users.Count(u => u.Role == "Clinician"),
                OpenAlerts = openAlerts.Count,
                UpcomingAppointments = upcomingAppointments.Count,

                AlertsToday = alertsToday,
                HighAlerts = highAlerts,
                MediumAlerts = mediumAlerts,
                LowAlerts = lowAlerts,
                TopPatientsByAlerts = topPatients,

                Patients = patients,
                Clinicians = clinicians,
                Appointments = upcomingAppointments,
                Alerts = openAlerts,
                ClinicianWorkload = clinicianWorkload,

                Search = search,
                RoleFilter = string.IsNullOrEmpty(roleFilter) ? "All" : roleFilter,
                ShowInactive = showInactive,

                HighPressureThreshold = _highPressureThreshold,
                NoMovementMinutes = _noMovementMinutes,

                AdminLog = _adminLog.AsEnumerable().Reverse().Take(10)
            };

            ViewData["Title"] = "Admin Dashboard";
            return View(vm);
        }

        // ----------------- ACTIONS: CREATE USER (GENERIC) -----------------
        [HttpPost]
        public IActionResult CreateUser(string fullName, string email, string role)
        {
            if (!IsAdmin)
                return RedirectToAction("AccessDenied", "Account");

            if (string.IsNullOrWhiteSpace(fullName) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(role))
            {
                TempData["AdminMessage"] = "Please fill in all user details.";
                return RedirectToAction("Admin");
            }

            var user = new AdminUserSummary
            {
                Id = _nextUserId++,
                FullName = fullName.Trim(),
                Email = email.Trim(),
                Role = role.Trim(),
                IsAvailable = role == "Clinician",
                IsActive = true
            };

            _users.Add(user);
            LogAdmin($"Created user '{user.FullName}' as {user.Role}.");
            TempData["AdminMessage"] = $"User '{fullName}' created as {role}.";

            return RedirectToAction("Admin");
        }

        // ----------------- ACTION: QUICK ADD CLINICIAN -----------------
        [HttpPost]
        public IActionResult CreateClinician(string fullName, string email)
        {
            if (!IsAdmin)
                return RedirectToAction("AccessDenied", "Account");

            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email))
            {
                TempData["AdminMessage"] = "Clinician name and email are required.";
                return RedirectToAction("Admin");
            }

            var clinician = new AdminUserSummary
            {
                Id = _nextUserId++,
                FullName = fullName.Trim(),
                Email = email.Trim(),
                Role = "Clinician",
                IsAvailable = true,
                IsActive = true
            };

            _users.Add(clinician);
            LogAdmin($"Added clinician '{clinician.FullName}'.");
            TempData["AdminMessage"] = $"Clinician '{fullName}' added to the team.";

            return RedirectToAction("Admin");
        }

        // ----------------- ACTION: DELETE PATIENT -----------------
        [HttpPost]
        public IActionResult DeletePatient(int id)
        {
            if (!IsAdmin)
                return RedirectToAction("AccessDenied", "Account");

            var patient = _users.FirstOrDefault(u => u.Id == id && u.Role == "Patient");
            if (patient != null)
            {
                _users.Remove(patient);

                _appointments.RemoveAll(a => a.PatientName == patient.FullName);
                _alerts.RemoveAll(a => a.PatientName == patient.FullName);

                LogAdmin($"Deleted patient '{patient.FullName}' and related appointments/alerts.");
                TempData["AdminMessage"] = $"Patient '{patient.FullName}' has been removed from the system.";
            }

            return RedirectToAction("Admin");
        }

        // ----------------- ACTION: SOFT ACTIVATE / DEACTIVATE USER -----------------
        [HttpPost]
        public IActionResult ToggleUserActive(int id)
        {
            if (!IsAdmin)
                return RedirectToAction("AccessDenied", "Account");

            var user = _users.FirstOrDefault(u => u.Id == id);
            if (user != null)
            {
                user.IsActive = !user.IsActive;
                LogAdmin($"{(user.IsActive ? "Reactivated" : "Deactivated")} user '{user.FullName}'.");
                TempData["AdminMessage"] = $"User '{user.FullName}' is now {(user.IsActive ? "active" : "inactive")}.";
            }

            return RedirectToAction("Admin");
        }

        // ----------------- ACTIONS: SCHEDULE APPOINTMENT -----------------
        [HttpPost]
        public IActionResult ScheduleAppointment(string patientName, string clinicianName, DateTime startTime, DateTime endTime)
        {
            if (!IsAdmin)
                return RedirectToAction("AccessDenied", "Account");

            if (string.IsNullOrWhiteSpace(patientName) ||
                string.IsNullOrWhiteSpace(clinicianName) ||
                startTime == default ||
                endTime == default ||
                endTime <= startTime)
            {
                TempData["AdminMessage"] = "Please provide valid appointment details.";
                return RedirectToAction("Admin");
            }

            var appointment = new AppointmentItem
            {
                Id = _nextAppointmentId++,
                PatientName = patientName.Trim(),
                ClinicianName = clinicianName.Trim(),
                StartTime = startTime,
                EndTime = endTime,
                Status = "Scheduled"
            };

            _appointments.Add(appointment);
            LogAdmin($"Scheduled appointment for {appointment.PatientName} with {appointment.ClinicianName}.");
            TempData["AdminMessage"] = $"Appointment scheduled for {patientName} with {clinicianName}.";

            return RedirectToAction("Admin");
        }

        // ----------------- ACTIONS: TOGGLE CLINICIAN AVAILABILITY -----------------
        [HttpPost]
        public IActionResult ToggleClinicianAvailability(int id)
        {
            if (!IsAdmin)
                return RedirectToAction("AccessDenied", "Account");

            var clinician = _users.FirstOrDefault(u => u.Id == id && u.Role == "Clinician");
            if (clinician != null)
            {
                clinician.IsAvailable = !clinician.IsAvailable;
                LogAdmin($"Set clinician '{clinician.FullName}' availability to {(clinician.IsAvailable ? "available" : "unavailable")}.");
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
                return RedirectToAction("AccessDenied", "Account");

            var alert = _alerts.FirstOrDefault(a => a.Id == id);
            if (alert != null)
            {
                alert.IsResolved = true;
                LogAdmin($"Resolved alert #{alert.Id} for {alert.PatientName}.");
                TempData["AdminMessage"] = $"Alert for {alert.PatientName} has been marked as resolved.";
            }

            return RedirectToAction("Admin");
        }

        // ----------------- ACTION: CREATE ALERT -----------------
        [HttpPost]
        public IActionResult CreateAlert(string patientName, string severity, string message)
        {
            if (!IsAdmin)
                return RedirectToAction("AccessDenied", "Account");

            if (string.IsNullOrWhiteSpace(patientName) ||
                string.IsNullOrWhiteSpace(severity) ||
                string.IsNullOrWhiteSpace(message))
            {
                TempData["AdminMessage"] = "Please fill in all alert details.";
                return RedirectToAction("Admin");
            }

            var alert = new AlertItem
            {
                Id = _nextAlertId++,
                PatientName = patientName.Trim(),
                Severity = severity.Trim(),
                Message = message.Trim(),
                RaisedAt = DateTime.Now,
                IsResolved = false
            };

            _alerts.Add(alert);
            LogAdmin($"Created {alert.Severity} alert for {alert.PatientName}.");
            TempData["AdminMessage"] = $"Alert created for {patientName} ({severity}).";

            return RedirectToAction("Admin");
        }

        // ----------------- ACTION: UPDATE SETTINGS -----------------
        [HttpPost]
        public IActionResult UpdateSettings(int highPressureThreshold, int noMovementMinutes)
        {
            if (!IsAdmin)
                return RedirectToAction("AccessDenied", "Account");

            if (highPressureThreshold <= 0 || noMovementMinutes <= 0)
            {
                TempData["AdminMessage"] = "Please provide positive values for settings.";
                return RedirectToAction("Admin");
            }

            _highPressureThreshold = highPressureThreshold;
            _noMovementMinutes = noMovementMinutes;

            LogAdmin($"Updated settings: HighPressureThreshold={_highPressureThreshold}, NoMovementMinutes={_noMovementMinutes}.");
            TempData["AdminMessage"] = "System settings updated.";

            return RedirectToAction("Admin");
        }
    }
}
