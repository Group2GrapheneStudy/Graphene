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

        public class PrescriptionItem
        {
            public int Id { get; set; }
            public string PatientName { get; set; } = string.Empty;
            public string ClinicianName { get; set; } = string.Empty;
            public string Medication { get; set; } = string.Empty;
            public string Dosage { get; set; } = string.Empty;
            public string Notes { get; set; } = string.Empty;
            public DateTime CreatedAt { get; set; }
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

        public class PatientFeedbackItem
        {
            public int Id { get; set; }
            public string PatientName { get; set; } = string.Empty;
            public string ClinicianName { get; set; } = string.Empty;
            public int Rating { get; set; } // 1–5
            public string Comment { get; set; } = string.Empty;
            public DateTime CreatedAt { get; set; }
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

        public class ClinicianViewModel
        {
            public string ClinicianName { get; set; } = string.Empty;
            public string SelectedClinician { get; set; } = string.Empty;
            public List<string> Clinicians { get; set; } = new();

            public int TodayAppointmentsCount { get; set; }
            public int UpcomingAppointmentsCount { get; set; }

            public List<AppointmentItem> TodayAppointments { get; set; } = new();
            public List<AppointmentItem> UpcomingAppointments { get; set; } = new();

            public List<string> Patients { get; set; } = new();

            public List<AlertItem> ActiveAlerts { get; set; } = new();
            public int HighAlerts { get; set; }
            public int MediumAlerts { get; set; }
            public int LowAlerts { get; set; }

            public List<PrescriptionItem> Prescriptions { get; set; } = new();
        }

        public class PatientViewModel
        {
            public string PatientName { get; set; } = string.Empty;

            public List<AppointmentItem> UpcomingAppointments { get; set; } = new();
            public List<PrescriptionItem> Prescriptions { get; set; } = new();

            public List<AlertItem> ActiveAlerts { get; set; } = new();
            public List<AlertItem> RecentAlerts { get; set; } = new();

            public int PressureRiskScore { get; set; } // 0–100
            public string PressureRiskLabel { get; set; } = string.Empty;
            public List<int> PressureTrend { get; set; } = new(); // simple numbers for UI

            public List<PatientFeedbackItem> MyFeedback { get; set; } = new();
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

        private static readonly List<PrescriptionItem> _prescriptions = new();

        private static readonly List<PatientFeedbackItem> _feedbacks = new();

        private static int _nextUserId = _users.Count == 0 ? 1 : _users.Max(u => u.Id) + 1;
        private static int _nextAppointmentId = _appointments.Count == 0 ? 1 : _appointments.Max(a => a.Id) + 1;
        private static int _nextAlertId = _alerts.Count == 0 ? 1 : _alerts.Max(a => a.Id) + 1;
        private static int _nextPrescriptionId = 1;
        private static int _nextFeedbackId = 1;

        // System settings (configurable on admin screen)
        private static int _highPressureThreshold = 80;
        private static int _noMovementMinutes = 45;

        // Simple audit log
        private static readonly List<string> _adminLog = new();

        private void LogAdmin(string message)
        {
            var entry = $"{DateTime.Now:dd MMM HH:mm} - {message}";
            _adminLog.Add(entry);
            if (_adminLog.Count > 50)
            {
                _adminLog.RemoveAt(0);
            }
        }

        // ----------------- PATIENT DASHBOARD -----------------
        public IActionResult Patient()
        {
            if (!(IsPatient || IsClinician || IsAdmin))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            // We assume the patient name stored in session matches the names used in appointments/prescriptions.
            var patientName = HttpContext.Session.GetString("UserName") ?? "Patient";

            var today = DateTime.Today;

            var upcomingAppointments = _appointments
                .Where(a => a.PatientName == patientName && a.StartTime >= today)
                .OrderBy(a => a.StartTime)
                .ToList();

            var prescriptions = _prescriptions
                .Where(p => p.PatientName == patientName)
                .OrderByDescending(p => p.CreatedAt)
                .Take(10)
                .ToList();

            var activeAlerts = _alerts
                .Where(a => a.PatientName == patientName && !a.IsResolved)
                .OrderByDescending(a => a.RaisedAt)
                .ToList();

            var recentAlerts = _alerts
                .Where(a => a.PatientName == patientName)
                .OrderByDescending(a => a.RaisedAt)
                .Take(5)
                .ToList();

            // Simple "pressure risk score" based on active alerts
            var highCount = activeAlerts.Count(a => a.Severity == "High");
            var medCount = activeAlerts.Count(a => a.Severity == "Medium");
            var lowCount = activeAlerts.Count(a => a.Severity == "Low");

            var score = highCount * 30 + medCount * 15 + lowCount * 5;
            if (score > 100) score = 100;

            string riskLabel;
            if (score >= 70) riskLabel = "High risk – please follow repositioning advice and contact your clinician.";
            else if (score >= 40) riskLabel = "Moderate risk – keep an eye on your posture and pressure areas.";
            else riskLabel = "Low risk – readings look OK, continue normal care.";

            // Simple synthetic pressure trend (for UI effect)
            var pressureTrend = new List<int>();
            var baseValue = 40 + highCount * 10 + medCount * 5;
            var random = new Random(patientName.GetHashCode());
            for (int i = 0; i < 7; i++)
            {
                var jitter = random.Next(-5, 6);
                var val = Math.Clamp(baseValue + jitter + i, 30, 95);
                pressureTrend.Add(val);
            }

            var myFeedback = _feedbacks
                .Where(f => f.PatientName == patientName)
                .OrderByDescending(f => f.CreatedAt)
                .Take(10)
                .ToList();

            var vm = new PatientViewModel
            {
                PatientName = patientName,
                UpcomingAppointments = upcomingAppointments,
                Prescriptions = prescriptions,
                ActiveAlerts = activeAlerts,
                RecentAlerts = recentAlerts,
                PressureRiskScore = score,
                PressureRiskLabel = riskLabel,
                PressureTrend = pressureTrend,
                MyFeedback = myFeedback
            };

            ViewData["Title"] = "Patient Dashboard";
            return View(vm);
        }

        // ----------------- CLINICIAN DASHBOARD -----------------
        public IActionResult Clinician(string? selectedClinician)
        {
            if (!(IsClinician || IsAdmin))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var clinicianNames = _users
                .Where(u => u.Role == "Clinician" && u.IsActive)
                .Select(u => u.FullName)
                .OrderBy(n => n)
                .ToList();

            string effectiveClinician;
            if (!string.IsNullOrWhiteSpace(selectedClinician) && clinicianNames.Contains(selectedClinician))
            {
                effectiveClinician = selectedClinician;
            }
            else
            {
                var sessionName = HttpContext.Session.GetString("UserName");
                if (!string.IsNullOrWhiteSpace(sessionName) && clinicianNames.Contains(sessionName))
                {
                    effectiveClinician = sessionName;
                }
                else
                {
                    effectiveClinician = clinicianNames.FirstOrDefault() ?? "Clinician";
                }
            }

            var today = DateTime.Today;

            var todayAppointments = _appointments
                .Where(a => a.ClinicianName == effectiveClinician && a.StartTime.Date == today)
                .OrderBy(a => a.StartTime)
                .ToList();

            var upcomingAppointments = _appointments
                .Where(a => a.ClinicianName == effectiveClinician && a.StartTime.Date > today)
                .OrderBy(a => a.StartTime)
                .ToList();

            var patientNames = todayAppointments
                .Concat(upcomingAppointments)
                .Select(a => a.PatientName)
                .Distinct()
                .OrderBy(n => n)
                .ToList();

            var activeAlerts = _alerts
                .Where(a => !a.IsResolved && patientNames.Contains(a.PatientName))
                .OrderByDescending(a => a.RaisedAt)
                .ToList();

            var highAlerts = activeAlerts.Count(a => a.Severity == "High");
            var mediumAlerts = activeAlerts.Count(a => a.Severity == "Medium");
            var lowAlerts = activeAlerts.Count(a => a.Severity == "Low");

            var myPrescriptions = _prescriptions
                .Where(p => p.ClinicianName == effectiveClinician)
                .OrderByDescending(p => p.CreatedAt)
                .Take(10)
                .ToList();

            var vm = new ClinicianViewModel
            {
                ClinicianName = effectiveClinician,
                SelectedClinician = effectiveClinician,
                Clinicians = clinicianNames,
                TodayAppointments = todayAppointments,
                UpcomingAppointments = upcomingAppointments,
                TodayAppointmentsCount = todayAppointments.Count,
                UpcomingAppointmentsCount = upcomingAppointments.Count,
                Patients = patientNames,
                ActiveAlerts = activeAlerts,
                HighAlerts = highAlerts,
                MediumAlerts = mediumAlerts,
                LowAlerts = lowAlerts,
                Prescriptions = myPrescriptions
            };

            ViewData["Title"] = "Clinician Dashboard";
            return View(vm);
        }

        // ----------------- ADMIN DASHBOARD (MAIN PANEL) -----------------
        [HttpGet]
        public IActionResult Admin(string? search, string? roleFilter, bool showInactive = false)
        {
            if (!IsAdmin)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            IEnumerable<AdminUserSummary> userQuery = _users;
            if (!showInactive)
            {
                userQuery = userQuery.Where(u => u.IsActive);
            }

            if (!string.IsNullOrEmpty(roleFilter) && roleFilter != "All")
            {
                userQuery = userQuery.Where(u =>
                    u.Role.Equals(roleFilter, StringComparison.OrdinalIgnoreCase));
            }

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
                _prescriptions.RemoveAll(p => p.PatientName == patient.FullName);
                _feedbacks.RemoveAll(f => f.PatientName == patient.FullName);

                LogAdmin($"Deleted patient '{patient.FullName}' and related data.");
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

        // ----------------- ACTIONS: SCHEDULE APPOINTMENT (ADMIN + CLINICIAN) -----------------
        [HttpPost]
        public IActionResult ScheduleAppointment(string patientName, string clinicianName, DateTime startTime, DateTime endTime)
        {
            if (!IsAdmin && !IsClinician)
                return RedirectToAction("AccessDenied", "Account");

            if (string.IsNullOrWhiteSpace(patientName) ||
                string.IsNullOrWhiteSpace(clinicianName) ||
                startTime == default ||
                endTime == default ||
                endTime <= startTime)
            {
                if (IsAdmin)
                {
                    TempData["AdminMessage"] = "Please provide valid appointment details.";
                    return RedirectToAction("Admin");
                }
                else
                {
                    TempData["ClinicianMessage"] = "Please provide valid appointment details.";
                    return RedirectToAction("Clinician", new { selectedClinician = clinicianName });
                }
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

            if (IsAdmin)
            {
                TempData["AdminMessage"] = $"Appointment scheduled for {patientName} with {clinicianName}.";
                return RedirectToAction("Admin");
            }
            else
            {
                TempData["ClinicianMessage"] = $"Appointment scheduled for {patientName}.";
                return RedirectToAction("Clinician", new { selectedClinician = clinicianName });
            }
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

        // ----------------- ACTIONS: RESOLVE ALERT (ADMIN + CLINICIAN) -----------------
        [HttpPost]
        public IActionResult ResolveAlert(int id)
        {
            if (!IsAdmin && !IsClinician)
                return RedirectToAction("AccessDenied", "Account");

            var alert = _alerts.FirstOrDefault(a => a.Id == id);
            if (alert != null)
            {
                alert.IsResolved = true;
                LogAdmin($"Resolved alert #{alert.Id} for {alert.PatientName}.");

                if (IsAdmin)
                {
                    TempData["AdminMessage"] = $"Alert for {alert.PatientName} has been marked as resolved.";
                    return RedirectToAction("Admin");
                }
                else
                {
                    TempData["ClinicianMessage"] = $"Alert for {alert.PatientName} has been marked as resolved.";
                    return RedirectToAction("Clinician");
                }
            }

            return IsAdmin ? RedirectToAction("Admin") : RedirectToAction("Clinician");
        }

        // ----------------- ACTION: CREATE ALERT (ADMIN + CLINICIAN) -----------------
        [HttpPost]
        public IActionResult CreateAlert(string patientName, string severity, string message)
        {
            if (!IsAdmin && !IsClinician)
                return RedirectToAction("AccessDenied", "Account");

            if (string.IsNullOrWhiteSpace(patientName) ||
                string.IsNullOrWhiteSpace(severity) ||
                string.IsNullOrWhiteSpace(message))
            {
                if (IsAdmin)
                {
                    TempData["AdminMessage"] = "Please fill in all alert details.";
                    return RedirectToAction("Admin");
                }
                else
                {
                    TempData["ClinicianMessage"] = "Please fill in all alert details.";
                    return RedirectToAction("Clinician");
                }
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

            if (IsAdmin)
            {
                TempData["AdminMessage"] = $"Alert created for {patientName} ({severity}).";
                return RedirectToAction("Admin");
            }
            else
            {
                TempData["ClinicianMessage"] = $"Alert created for {patientName} ({severity}).";
                return RedirectToAction("Clinician");
            }
        }

        // ----------------- ACTION: CREATE PRESCRIPTION (CLINICIAN + ADMIN) -----------------
        [HttpPost]
        public IActionResult CreatePrescription(string patientName, string medication, string dosage, string? notes, string? clinicianName)
        {
            if (!IsClinician && !IsAdmin)
                return RedirectToAction("AccessDenied", "Account");

            var effectiveClinician = !string.IsNullOrWhiteSpace(clinicianName)
                ? clinicianName.Trim()
                : (HttpContext.Session.GetString("UserName") ?? "Clinician");

            if (string.IsNullOrWhiteSpace(patientName) ||
                string.IsNullOrWhiteSpace(medication) ||
                string.IsNullOrWhiteSpace(dosage))
            {
                TempData["ClinicianMessage"] = "Please provide patient name, medication and dosage.";
                return RedirectToAction("Clinician", new { selectedClinician = effectiveClinician });
            }

            var prescription = new PrescriptionItem
            {
                Id = _nextPrescriptionId++,
                PatientName = patientName.Trim(),
                ClinicianName = effectiveClinician,
                Medication = medication.Trim(),
                Dosage = dosage.Trim(),
                Notes = notes?.Trim() ?? string.Empty,
                CreatedAt = DateTime.Now
            };

            _prescriptions.Add(prescription);
            LogAdmin($"Prescription recorded by {effectiveClinician} for {patientName}: {medication} {dosage}.");

            TempData["ClinicianMessage"] = $"Prescription recorded for {patientName}.";
            return RedirectToAction("Clinician", new { selectedClinician = effectiveClinician });
        }

        // ----------------- ACTION: PATIENT FEEDBACK -----------------
        [HttpPost]
        public IActionResult LeaveFeedback(string clinicianName, int rating, string comment)
        {
            if (!IsPatient && !IsAdmin)
                return RedirectToAction("AccessDenied", "Account");

            var patientName = HttpContext.Session.GetString("UserName") ?? "Patient";

            if (string.IsNullOrWhiteSpace(clinicianName) ||
                rating < 1 || rating > 5 ||
                string.IsNullOrWhiteSpace(comment))
            {
                TempData["PatientMessage"] = "Please select clinician, rating (1-5) and enter a comment.";
                return RedirectToAction("Patient");
            }

            var feedback = new PatientFeedbackItem
            {
                Id = _nextFeedbackId++,
                PatientName = patientName,
                ClinicianName = clinicianName.Trim(),
                Rating = rating,
                Comment = comment.Trim(),
                CreatedAt = DateTime.Now
            };

            _feedbacks.Add(feedback);
            LogAdmin($"Feedback added by {patientName} for {feedback.ClinicianName} (rating {rating}/5).");
            TempData["PatientMessage"] = "Thank you for your feedback.";

            return RedirectToAction("Patient");
        }

        // ----------------- ACTION: UPDATE SETTINGS (ADMIN) -----------------
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
