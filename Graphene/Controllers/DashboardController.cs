using Graphene_Group_Project.Data;
using Graphene_Group_Project.Data.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using static GrapheneTrace.Web.Controllers.DashboardController.ClinicianViewModel;

namespace GrapheneTrace.Web.Controllers
{
    public class DashboardController : Controller
    {
        // DB CONTEXT
        private readonly AppDbContext _db;

        public DashboardController(AppDbContext db)
        {
            _db = db;
        }

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
            public string Severity { get; set; } = "Low"; // "Low", "Medium", "High"
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

            // Alert analytics
            public int AlertsToday { get; set; }
            public int HighAlerts { get; set; }
            public int MediumAlerts { get; set; }
            public int LowAlerts { get; set; }

            public List<PatientAlertSummary> TopPatientsByAlerts { get; set; } = new();

            // Required by Admin.cshtml
            public int HighPressureThreshold { get; set; }
            public int NoMovementMinutes { get; set; }

            public int OpenAlerts { get; set; }
            public int UpcomingAppointments { get; set; }

            // Main collections
            public List<AdminUserSummary> Patients { get; set; } = new();
            public List<AdminUserSummary> Clinicians { get; set; } = new();

            public List<AppointmentItem> Appointments { get; set; } = new();   // renamed from AllAppointments
            public List<AlertItem> Alerts { get; set; } = new();               // renamed from AllAlerts

            // Clinician workload blocks
            public List<ClinicianWorkloadInfo> ClinicianWorkload { get; set; } = new();

            // Feedback log
            public List<PatientFeedbackItem> RecentFeedback { get; set; } = new();

            // Audit log
            public List<string> AdminLog { get; set; } = new();

            // Filters from search bar
            public string Search { get; set; } = string.Empty;
            public string RoleFilter { get; set; } = "All";
            public bool ShowInactive { get; set; }
        }

        public class MessageItem
        {
            public int Id { get; set; }

            public int PatientId { get; set; }
            public string PatientName { get; set; } = string.Empty;
            public string ClinicianName { get; set; } = string.Empty;

            // "Patient" or "Clinician"
            public string FromRole { get; set; } = string.Empty;

            public string Text { get; set; } = string.Empty;
            public DateTime SentAt { get; set; }

            public bool IsReadByPatient { get; set; }
            public bool IsReadByClinician { get; set; }
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

        public class ClinicianViewModel
        {
            public class PatientOption
            {
                public int PatientId { get; set; }
                public string FullName { get; set; } = string.Empty;
            }

            public string ClinicianName { get; set; } = string.Empty;
            public string SelectedClinician { get; set; } = string.Empty;
            public List<string> Clinicians { get; set; } = new();

            public int TodayAppointmentsCount { get; set; }
            public int UpcomingAppointmentsCount { get; set; }

            public List<AppointmentItem> TodayAppointments { get; set; } = new();
            public List<AppointmentItem> UpcomingAppointments { get; set; } = new();
            public List<AppointmentItem> AllAppointments { get; set; } = new();

            public List<AlertItem> ActiveAlerts { get; set; } = new();

            public List<AdminUserSummary> Patients { get; set; } = new();
            public List<PrescriptionItem> RecentPrescriptions { get; set; } = new();

            public List<MessageItem> InboxMessages { get; set; } = new();

            public List<PatientOption> PatientOptions { get; set; } = new();
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

            // Existing simple numeric trend used by the chart
            public List<int> PressureTrend { get; set; } = new();

            // NEW: time range + metrics derived from PressureFrames
            public string TimeRange { get; set; } = "24h";
            public string TimeRangeLabel { get; set; } = "Last 24 hours";
            public int? CurrentPpi { get; set; }
            public decimal? CurrentContactArea { get; set; }

            public List<PatientFeedbackItem> MyFeedback { get; set; } = new();

            // Conversation for this patient
            public List<MessageItem> Conversation { get; set; } = new();
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
                ClinicianName = "Dr. Clark",
                StartTime = DateTime.Today.AddDays(1).AddHours(9),
                EndTime = DateTime.Today.AddDays(1).AddHours(9.5),
                Status = "Scheduled"
            },
            new AppointmentItem
            {
                Id = 3,
                PatientName = "Alice Patient",
                ClinicianName = "Dr. Dana",
                StartTime = DateTime.Today.AddDays(2).AddHours(14),
                EndTime = DateTime.Today.AddDays(2).AddHours(14.5),
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
                Message = "Sustained high pressure on left hip area.",
                RaisedAt = DateTime.Today.AddHours(-2),
                IsResolved = false
            },
            new AlertItem
            {
                Id = 2,
                PatientName = "Bob Patient",
                Severity = "Medium",
                Message = "Elevated pressure trend over the last 24 hours.",
                RaisedAt = DateTime.Today.AddHours(-5),
                IsResolved = false
            },
            new AlertItem
            {
                Id = 3,
                PatientName = "Alice Patient",
                Severity = "Low",
                Message = "Short spike in pressure – monitor.",
                RaisedAt = DateTime.Today.AddDays(-1),
                IsResolved = true
            }
        };

        private static readonly List<PrescriptionItem> _prescriptions = new()
        {
            new PrescriptionItem
            {
                Id = 1,
                PatientName = "Alice Patient",
                ClinicianName = "Dr. Clark",
                Medication = "Cushion X",
                Dosage = "4 hours/day",
                Notes = "Use while seated at desk",
                CreatedAt = DateTime.Today.AddDays(-3)
            },
            new PrescriptionItem
            {
                Id = 2,
                PatientName = "Bob Patient",
                ClinicianName = "Dr. Dana",
                Medication = "Reposition schedule",
                Dosage = "Every 30 minutes",
                Notes = "Set phone reminders",
                CreatedAt = DateTime.Today.AddDays(-1)
            }
        };

        private static readonly List<PatientFeedbackItem> _feedbacks = new();
        private static readonly List<MessageItem> _messages = new();

        private static int _nextUserId = _users.Count == 0 ? 1 : _users.Max(u => u.Id) + 1;
        private static int _nextAppointmentId = _appointments.Count == 0 ? 1 : _appointments.Max(a => a.Id) + 1;
        private static int _nextAlertId = _alerts.Count == 0 ? 1 : _alerts.Max(a => a.Id) + 1;
        private static int _nextPrescriptionId = _prescriptions.Count == 0 ? 1 : _prescriptions.Max(p => p.Id) + 1;
        private static int _nextFeedbackId = _feedbacks.Count == 0 ? 1 : _feedbacks.Max(f => f.Id) + 1;
        private static int _nextMessageId = _messages.Count == 0 ? 1 : _messages.Max(m => m.Id) + 1;

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
        public IActionResult Patient(string? range)
        {
            if (!(IsPatient || IsClinician || IsAdmin))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            // Normalise requested time range
            var selectedRange = string.IsNullOrWhiteSpace(range) ? "24h" : range;

            // Name stored in session
            var patientName = HttpContext.Session.GetString("UserName") ?? "Patient";
            var today = DateTime.Today;

            // Try to find the matching Patient row in the DB
            var dbPatient = _db.Patients.FirstOrDefault(p => p.FullName == patientName);
            var patientId = dbPatient?.PatientId ?? 0;

            // Demo appointments & prescriptions
            var upcomingAppointments = _appointments
                .Where(a => a.PatientName == patientName && a.StartTime >= DateTime.Now)
                .OrderBy(a => a.StartTime)
                .ToList();

            var prescriptions = _prescriptions
                .Where(p => p.PatientName == patientName)
                .OrderByDescending(p => p.CreatedAt)
                .Take(10)
                .ToList();

            // In-memory alerts
            var activeAlerts = _alerts
                .Where(a => a.PatientName == patientName && !a.IsResolved)
                .OrderByDescending(a => a.RaisedAt)
                .ToList();

            var recentAlerts = _alerts
                .Where(a => a.PatientName == patientName)
                .OrderByDescending(a => a.RaisedAt)
                .Take(5)
                .ToList();

            // Merge DB alerts for this patient
            if (dbPatient != null)
            {
                var dbAlerts = _db.Alerts
                    .Where(a => a.PatientId == dbPatient.PatientId)
                    .OrderByDescending(a => a.TriggeredUtc)
                    .ToList();

                var dbAlertItems = dbAlerts.Select(a => new AlertItem
                {
                    Id = (int)a.AlertId,
                    PatientName = patientName,
                    Severity = a.Severity switch
                    {
                        3 => "High",
                        2 => "Medium",
                        1 => "Low",
                        _ => "Low"
                    },
                    Message = a.MaxPressure.HasValue
                        ? $"Auto alert – peak {a.MaxPressure} (pixels ≥ thr: {a.PixelsAboveThr ?? 0})."
                        : "Auto alert from pressure sensor.",
                    RaisedAt = a.TriggeredUtc,
                    IsResolved = a.Status == 2
                }).ToList();

                activeAlerts.AddRange(dbAlertItems.Where(a => !a.IsResolved));
                recentAlerts = activeAlerts
                    .OrderByDescending(a => a.RaisedAt)
                    .Take(5)
                    .ToList();
            }

            // Simple "pressure risk score" based on active alerts
            var highCount = activeAlerts.Count(a => a.Severity == "High");
            var medCount = activeAlerts.Count(a => a.Severity == "Medium");
            var lowCount = activeAlerts.Count(a => a.Severity == "Low");

            var score = highCount * 30 + medCount * 15 + lowCount * 5;
            if (score > 100) score = 100;

            string riskLabel;
            if (score >= 70)
                riskLabel = "High risk – please follow repositioning advice and contact your clinician.";
            else if (score >= 40)
                riskLabel = "Moderate risk – keep an eye on your posture and pressure areas.";
            else
                riskLabel = "Low risk – readings look OK, continue normal care.";

            // NEW: pull real metrics from PressureFrames where available
            int? currentPpi = null;
            decimal? currentContactArea = null;
            var pressureTrend = new List<int>();

            string rangeLabel = selectedRange switch
            {
                "1h" => "Last 1 hour",
                "6h" => "Last 6 hours",
                "24h" => "Last 24 hours",
                _ => "All available data"
            };

            if (dbPatient != null)
            {
                DateTime? sinceUtc = selectedRange switch
                {
                    "1h" => DateTime.UtcNow.AddHours(-1),
                    "6h" => DateTime.UtcNow.AddHours(-6),
                    "24h" => DateTime.UtcNow.AddHours(-24),
                    _ => null
                };

                var framesQuery = _db.PressureFrames.Where(f => f.PatientId == dbPatient.PatientId);

                if (sinceUtc.HasValue)
                {
                    framesQuery = framesQuery.Where(f => (f.CapturedUtc ?? f.CreatedUtc) >= sinceUtc.Value);
                }

                var frames = framesQuery
                    .OrderBy(f => f.CapturedUtc ?? f.CreatedUtc)
                    .Take(50)
                    .ToList();

                if (frames.Any())
                {
                    // Use PeakPressure as the numeric series for the existing chart
                    pressureTrend = frames
                        .Select(f => f.PeakPressure ?? 0)
                        .ToList();

                    var last = frames.Last();
                    currentPpi = last.PeakPressure;
                    if (last.ContactAreaPct.HasValue)
                    {
                        currentContactArea = last.ContactAreaPct.Value;
                    }
                }
            }

            // If we had no frames or all PeakPressure were null, fall back to the original synthetic trend
            if (!pressureTrend.Any())
            {
                var baseValue = 40 + highCount * 10 + medCount * 5;
                var random = new Random(patientName.GetHashCode());
                for (int i = 0; i < 7; i++)
                {
                    var jitter = random.Next(-5, 6);
                    var val = Math.Clamp(baseValue + jitter + i, 30, 95);
                    pressureTrend.Add(val);
                }
            }

            var myFeedback = _feedbacks
                .Where(f => f.PatientName == patientName)
                .OrderByDescending(f => f.CreatedAt)
                .Take(10)
                .ToList();

            // Load conversation for this patient (by PatientId) from in-memory list
            var conversation = new List<MessageItem>();
            if (patientId != 0)
            {
                conversation = _messages
                    .Where(m => m.PatientId == patientId)
                    .OrderBy(m => m.SentAt)
                    .ToList();

                // Mark clinician messages as read
                foreach (var msg in conversation.Where(m => m.FromRole == "Clinician"))
                {
                    msg.IsReadByPatient = true;
                }
            }

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
                TimeRange = selectedRange,
                TimeRangeLabel = rangeLabel,
                CurrentPpi = currentPpi,
                CurrentContactArea = currentContactArea,
                MyFeedback = myFeedback,
                Conversation = conversation
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

            var clinicianName = HttpContext.Session.GetString("UserName") ?? clinicianNames.FirstOrDefault() ?? "Clinician";

            if (string.IsNullOrWhiteSpace(selectedClinician))
            {
                selectedClinician = clinicianName;
            }

            var vm = new ClinicianViewModel
            {
                ClinicianName = clinicianName,
                SelectedClinician = selectedClinician,
                Clinicians = clinicianNames
            };

            // Today's appointments for this clinician
            var today = DateTime.Today;
            vm.TodayAppointments = _appointments
                .Where(a => a.ClinicianName == selectedClinician && a.StartTime.Date == today)
                .OrderBy(a => a.StartTime)
                .ToList();

            vm.TodayAppointmentsCount = vm.TodayAppointments.Count;

            // Upcoming appointments (next few days)
            vm.UpcomingAppointments = _appointments
                .Where(a => a.ClinicianName == selectedClinician && a.StartTime.Date > today)
                .OrderBy(a => a.StartTime)
                .ToList();

            vm.UpcomingAppointmentsCount = vm.UpcomingAppointments.Count;

            vm.AllAppointments = _appointments
                .Where(a => a.ClinicianName == selectedClinician)
                .OrderBy(a => a.StartTime)
                .ToList();

            // Active alerts for this clinician's patients (simple demo logic)
            var myPatientNames = _appointments
                .Where(a => a.ClinicianName == selectedClinician)
                .Select(a => a.PatientName)
                .Distinct()
                .ToList();

            vm.ActiveAlerts = _alerts
                .Where(a => !a.IsResolved && myPatientNames.Contains(a.PatientName))
                .OrderByDescending(a => a.RaisedAt)
                .ToList();

            // Patients under care
            vm.Patients = _users
                .Where(u => u.Role == "Patient" && u.IsActive)
                .ToList();

            // Recent prescriptions issued by this clinician
            vm.RecentPrescriptions = _prescriptions
                .Where(p => p.ClinicianName == selectedClinician)
                .OrderByDescending(p => p.CreatedAt)
                .Take(10)
                .ToList();

            // Basic patient options (for messaging etc.)
            if (myPatientNames.Any())
            {
                var dbPatients = _db.Patients
                    .Where(p => myPatientNames.Contains(p.FullName))
                    .Select(p => new ClinicianViewModel.PatientOption
                    {
                        PatientId = p.PatientId,
                        FullName = p.FullName
                    })
                    .OrderBy(p => p.FullName)
                    .ToList();

                vm.PatientOptions = dbPatients;
            }

            // Inbox messages (all messages, newest first)
            vm.InboxMessages = _messages
                .OrderByDescending(m => m.SentAt)
                .ToList();

            // Mark patient messages as read by clinician
            foreach (var msg in vm.InboxMessages.Where(m => m.FromRole == "Patient"))
            {
                msg.IsReadByClinician = true;
            }

            ViewData["Title"] = "Clinician Dashboard";
            return View(vm);
        }

        // ----------------- ADMIN DASHBOARD (MAIN PAGE) -----------------
        public IActionResult Admin(string? search, string roleFilter = "All", bool showInactive = false)
        {
            if (!IsAdmin)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var vm = new AdminViewModel
            {
                Search = search ?? string.Empty,
                RoleFilter = roleFilter,
                ShowInactive = showInactive
            };

            var patients = _users.Where(u => u.Role == "Patient").ToList();
            var clinicians = _users.Where(u => u.Role == "Clinician").ToList();

            vm.TotalPatients = patients.Count;
            vm.TotalClinicians = clinicians.Count;
            vm.TotalUsers = _users.Count;

            var today = DateTime.Today;
            var openAlerts = _alerts.Where(a => !a.IsResolved).OrderByDescending(a => a.RaisedAt).ToList();
            var upcomingAppointments = _appointments
                .Where(a => a.StartTime >= today)
                .OrderBy(a => a.StartTime)
                .ToList();

            vm.OpenAlerts = openAlerts.Count;
            vm.UpcomingAppointments = upcomingAppointments.Count;

            vm.Alerts = openAlerts;
            vm.Appointments = upcomingAppointments;

            vm.AlertsToday = openAlerts.Count(a => a.RaisedAt.Date == today);
            vm.HighAlerts = openAlerts.Count(a => a.Severity == "High");
            vm.MediumAlerts = openAlerts.Count(a => a.Severity == "Medium");
            vm.LowAlerts = openAlerts.Count(a => a.Severity == "Low");

            vm.TopPatientsByAlerts = openAlerts
                .GroupBy(a => a.PatientName)
                .Select(g => new PatientAlertSummary
                {
                    PatientName = g.Key ?? "Unknown",
                    OpenAlertCount = g.Count()
                })
                .OrderByDescending(x => x.OpenAlertCount)
                .Take(5)
                .ToList();

            vm.RecentFeedback = _feedbacks
                .OrderByDescending(f => f.CreatedAt)
                .Take(10)
                .ToList();

            vm.ClinicianWorkload = clinicians.Select(c =>
            {
                var count = upcomingAppointments.Count(a => a.ClinicianName == c.FullName);
                var label = "Light";
                if (count >= 5) label = "Busy";
                else if (count >= 2) label = "Normal";

                return new ClinicianWorkloadInfo
                {
                    Name = c.FullName,
                    UpcomingAppointments = count,
                    LoadLabel = label
                };
            }).ToList();

            // Apply search / filters to user lists
            var userQuery = _users.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                userQuery = userQuery.Where(u =>
                    u.FullName.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                    u.Email.Contains(s, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.Equals(roleFilter, "All", StringComparison.OrdinalIgnoreCase))
            {
                userQuery = userQuery.Where(u => u.Role.Equals(roleFilter, StringComparison.OrdinalIgnoreCase));
            }

            if (!showInactive)
            {
                userQuery = userQuery.Where(u => u.IsActive);
            }

            var filteredUsers = userQuery.ToList();
            vm.Patients = filteredUsers.Where(u => u.Role == "Patient").ToList();
            vm.Clinicians = filteredUsers.Where(u => u.Role == "Clinician").ToList();

            vm.AdminLog = _adminLog.ToList();

            ViewData["Title"] = "Admin Dashboard";
            return View(vm);
        }

        // ----------------- ACTION: RESOLVE ALERT -----------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ResolveAlert(int id)
        {
            if (!IsClinician && !IsAdmin)
                return RedirectToAction("AccessDenied", "Account");

            var alert = _alerts.FirstOrDefault(a => a.Id == id);
            if (alert != null)
            {
                alert.IsResolved = true;
                LogAdmin($"Alert {id} marked as resolved.");
            }

            return RedirectToAction("Clinician");
        }

        // ----------------- ACTION: ADD PRESCRIPTION -----------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddPrescription(string patientName, string clinicianName, string medication, string dosage, string notes)
        {
            if (!IsClinician && !IsAdmin)
                return RedirectToAction("AccessDenied", "Account");

            if (string.IsNullOrWhiteSpace(patientName) ||
                string.IsNullOrWhiteSpace(clinicianName) ||
                string.IsNullOrWhiteSpace(medication))
            {
                TempData["ClinicianMessage"] = "Please fill in patient, clinician and medication.";
                return RedirectToAction("Clinician");
            }

            var item = new PrescriptionItem
            {
                Id = _nextPrescriptionId++,
                PatientName = patientName.Trim(),
                ClinicianName = clinicianName.Trim(),
                Medication = medication.Trim(),
                Dosage = dosage?.Trim() ?? string.Empty,
                Notes = notes?.Trim() ?? string.Empty,
                CreatedAt = DateTime.Now
            };

            _prescriptions.Add(item);
            LogAdmin($"Prescription added for {item.PatientName} by {item.ClinicianName}.");

            TempData["ClinicianMessage"] = "Prescription added.";
            return RedirectToAction("Clinician");
        }

        // ----------------- ACTION: LEAVE FEEDBACK -----------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult LeaveFeedback(string clinicianName, int rating, string comments)
        {
            if (!IsPatient)
                return RedirectToAction("AccessDenied", "Account");

            var patientName = HttpContext.Session.GetString("UserName") ?? "Unknown";

            if (string.IsNullOrWhiteSpace(clinicianName) || rating < 1 || rating > 5)
            {
                TempData["PatientMessage"] = "Please select a clinician and a rating between 1 and 5.";
                return RedirectToAction("Patient");
            }

            var feedback = new PatientFeedbackItem
            {
                Id = _nextFeedbackId++,
                PatientName = patientName,
                ClinicianName = clinicianName.Trim(),
                Rating = rating,
                Comment = comments?.Trim() ?? string.Empty,
                CreatedAt = DateTime.Now
            };

            _feedbacks.Add(feedback);
            LogAdmin($"Feedback added by {patientName} for {feedback.ClinicianName} (rating {rating}/5).");
            TempData["PatientMessage"] = "Thank you for your feedback.";

            return RedirectToAction("Patient");
        }

        // ----------------- ACTION: PATIENT → CLINICIAN MESSAGE -----------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SendMessageToClinician(string text)
        {
            if (!IsPatient)
                return RedirectToAction("AccessDenied", "Account");

            var patientName = HttpContext.Session.GetString("UserName") ?? "Unknown";

            var patientEntity = _db.Patients.FirstOrDefault(p => p.FullName == patientName);
            if (patientEntity == null)
            {
                TempData["PatientMessage"] = "Patient record not found.";
                return RedirectToAction("Patient");
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                TempData["PatientMessage"] = "Please enter a message.";
                return RedirectToAction("Patient");
            }

            var msg = new MessageItem
            {
                Id = _nextMessageId++,
                PatientId = patientEntity.PatientId,
                PatientName = patientEntity.FullName,
                ClinicianName = "Clinician",
                FromRole = "Patient",
                Text = text.Trim(),
                SentAt = DateTime.Now,
                IsReadByPatient = true,
                IsReadByClinician = false
            };

            _messages.Add(msg);

            TempData["PatientMessage"] = "Message sent to your clinician.";
            return RedirectToAction("Patient");
        }

        // ----------------- ACTION: CLINICIAN → PATIENT MESSAGE -----------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SendMessageToPatient(int patientId, string text)
        {
            if (!IsClinician && !IsAdmin)
                return RedirectToAction("AccessDenied", "Account");

            if (patientId <= 0 || string.IsNullOrWhiteSpace(text))
            {
                TempData["ClinicianMessage"] = "Enter patient ID and a message.";
                return RedirectToAction("Clinician");
            }

            var clinicianName = HttpContext.Session.GetString("UserName") ?? "Clinician";

            var patientEntity = _db.Patients.FirstOrDefault(p => p.PatientId == patientId);
            if (patientEntity == null)
            {
                TempData["ClinicianMessage"] = "Patient not found.";
                return RedirectToAction("Clinician");
            }

            var msg = new MessageItem
            {
                Id = _nextMessageId++,
                PatientId = patientEntity.PatientId,
                PatientName = patientEntity.FullName,
                ClinicianName = clinicianName,
                FromRole = "Clinician",
                Text = text.Trim(),
                SentAt = DateTime.Now,
                IsReadByPatient = false,
                IsReadByClinician = true
            };

            _messages.Add(msg);

            TempData["ClinicianMessage"] = $"Message sent to {patientEntity.FullName}.";
            return RedirectToAction("Clinician");
        }

        // ----------------- ACTION: UPDATE SETTINGS -----------------
        [HttpPost]
        [ValidateAntiForgeryToken]
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

        // ----------------- ACTION: TOGGLE USER ACTIVE -----------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ToggleUserActive(int id)
        {
            if (!IsAdmin)
                return RedirectToAction("AccessDenied", "Account");

            var user = _users.FirstOrDefault(u => u.Id == id);
            if (user != null)
            {
                user.IsActive = !user.IsActive;
                LogAdmin($"User {user.FullName} ({user.Role}) active={user.IsActive}.");
            }

            return RedirectToAction("Admin");
        }

        // ----------------- ACTION: DELETE PATIENT (DEMO ONLY) -----------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeletePatient(int id)
        {
            if (!IsAdmin)
                return RedirectToAction("AccessDenied", "Account");

            var user = _users.FirstOrDefault(u => u.Id == id && u.Role == "Patient");
            if (user != null)
            {
                _users.Remove(user);
                _appointments.RemoveAll(a => a.PatientName == user.FullName);
                _alerts.RemoveAll(a => a.PatientName == user.FullName);

                LogAdmin($"Patient {user.FullName} deleted (demo only, DB rows are not removed).");
            }

            return RedirectToAction("Admin");
        }
    }
}
