using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GrapheneTrace.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GrapheneTrace.Web.Controllers
{
    public class DashboardController : Controller
    {
        // ------------- ROLE HELPERS -------------

        private string? UserRole => HttpContext.Session.GetString("UserRole");
        private bool IsAdmin => UserRole == "Admin";
        private bool IsClinician => UserRole == "Clinician";
        private bool IsPatient => UserRole == "Patient";

        private readonly PressureCsvService _csvService;

        public DashboardController(PressureCsvService csvService)
        {
            _csvService = csvService;
        }

        // ------------- NESTED MODELS -------------

        public class AdminUserSummary
        {
            public int Id { get; set; }
            public string FullName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Role { get; set; } = string.Empty; // "Patient", "Clinician", "Admin"
            public bool IsAvailable { get; set; }            // for clinicians
            public bool IsActive { get; set; } = true;       // soft deactivation

            // For patients – which clinician they are assigned to
            public string? AssignedClinicianName { get; set; }
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
            // stats
            public int TotalUsers { get; set; }
            public int TotalPatients { get; set; }
            public int TotalClinicians { get; set; }
            public int OpenAlerts { get; set; }
            public int UpcomingAppointments { get; set; }

            // alert analytics
            public int AlertsToday { get; set; }
            public int HighAlerts { get; set; }
            public int MediumAlerts { get; set; }
            public int LowAlerts { get; set; }
            public List<PatientAlertSummary> TopPatientsByAlerts { get; set; } = new();

            // lists
            public List<AdminUserSummary> Patients { get; set; } = new();
            public List<AdminUserSummary> Clinicians { get; set; } = new();
            public List<AppointmentItem> Appointments { get; set; } = new();
            public List<AlertItem> Alerts { get; set; } = new();
            public List<ClinicianWorkloadInfo> ClinicianWorkload { get; set; } = new();

            // filters
            public string? Search { get; set; }
            public string? RoleFilter { get; set; }
            public bool ShowInactive { get; set; }

            // settings
            public int HighPressureThreshold { get; set; }
            public int NoMovementMinutes { get; set; }

            // audit
            public IEnumerable<string> AdminLog { get; set; } = Enumerable.Empty<string>();
        }

        public class ClinicianViewModel
        {
            public class PatientSensorSnapshot
            {
                public string PatientName { get; set; } = string.Empty;
                public int? LatestPeak { get; set; }
                public double? LatestAverage { get; set; }
            }

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
            public List<PatientSensorSnapshot> PatientSensorSnapshots { get; set; } = new();
        }

        public class PatientViewModel
        {
            public string PatientName { get; set; } = string.Empty;

            public string? AssignedClinicianName { get; set; }

            public List<AppointmentItem> UpcomingAppointments { get; set; } = new();
            public List<PrescriptionItem> Prescriptions { get; set; } = new();

            public List<AlertItem> ActiveAlerts { get; set; } = new();
            public List<AlertItem> RecentAlerts { get; set; } = new();

            public int PressureRiskScore { get; set; } // 0–100
            public string PressureRiskLabel { get; set; } = string.Empty;
            public List<int> PressureTrend { get; set; } = new();

            public List<PatientFeedbackItem> MyFeedback { get; set; } = new();

            // CSV sensor snapshot
            public string? SensorSessionId { get; set; }
            public int? LatestFrameIndex { get; set; }
            public int[]? LatestFrameValues { get; set; }
            public double? LatestFrameAverage { get; set; }
            public int? LatestFrameMax { get; set; }
        }

        // ------------- STATIC DATA (IN-MEMORY) -------------

        // main user list shown in admin dashboard
        private static readonly List<AdminUserSummary> _users = new()
        {
            new AdminUserSummary { Id = 1, FullName = "Patient 1", Email = "patient1@example.com", Role = "Patient", IsAvailable = true,  IsActive = true, AssignedClinicianName = "Dr. Clark" },
            new AdminUserSummary { Id = 2, FullName = "Patient 2", Email = "patient2@example.com", Role = "Patient", IsAvailable = true,  IsActive = true, AssignedClinicianName = "Dr. Clark" },
            new AdminUserSummary { Id = 3, FullName = "Patient 3", Email = "patient3@example.com", Role = "Patient", IsAvailable = true,  IsActive = true, AssignedClinicianName = "Dr. Dana"  },
            new AdminUserSummary { Id = 4, FullName = "Patient 4", Email = "patient4@example.com", Role = "Patient", IsAvailable = true,  IsActive = true, AssignedClinicianName = "Dr. Dana"  },
            new AdminUserSummary { Id = 5, FullName = "Patient 5", Email = "patient5@example.com", Role = "Patient", IsAvailable = true,  IsActive = true, AssignedClinicianName = "Dr. Clark" },
            new AdminUserSummary { Id = 6, FullName = "Dr. Clark",   Email = "clark@example.com",   Role = "Clinician", IsAvailable = true,  IsActive = true },
            new AdminUserSummary { Id = 7, FullName = "Dr. Dana",    Email = "dana@example.com",    Role = "Clinician", IsAvailable = true,  IsActive = true },
            new AdminUserSummary { Id = 8, FullName = "System Admin",Email = "admin@example.com",   Role = "Admin",     IsAvailable = true,  IsActive = true }
        };

        // patient → supervising clinician
        private static readonly Dictionary<string, string> _patientClinicianMap = new()
        {
            { "Patient 1", "Dr. Clark" },
            { "Patient 2", "Dr. Clark" },
            { "Patient 3", "Dr. Dana"  },
            { "Patient 4", "Dr. Dana"  },
            { "Patient 5", "Dr. Clark" }
        };

        // 5 patients → 15 CSV sessions (3 per patient)
        private static readonly Dictionary<string, List<string>> _patientSessionMap = new()
        {
            { "Patient 1", new List<string> { "d13043b3_20251011", "d13043b3_20251012", "d13043b3_20251013" } },
            { "Patient 2", new List<string> { "de0e9b2c_20251011", "de0e9b2c_20251012", "de0e9b2c_20251013" } },
            { "Patient 3", new List<string> { "1c0fd777_20251011", "1c0fd777_20251012", "1c0fd777_20251013" } },
            { "Patient 4", new List<string> { "71e66ab3_20251011", "71e66ab3_20251012", "71e66ab3_20251013" } },
            { "Patient 5", new List<string> { "543d4676_20251011", "543d4676_20251012", "543d4676_20251013" } }
        };

        // appointments / alerts / prescriptions / feedback
        private static readonly List<AppointmentItem> _appointments = new();
        private static readonly List<AlertItem> _alerts = new();
        private static readonly List<PrescriptionItem> _prescriptions = new();
        private static readonly List<PatientFeedbackItem> _feedbacks = new();

        private static int _nextUserId = _users.Max(u => u.Id) + 1;
        private static int _nextAppointmentId = 1;
        private static int _nextAlertId = 1;
        private static int _nextPrescriptionId = 1;
        private static int _nextFeedbackId = 1;

        // System settings
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

        // Helper for AccountController: register new user into in-memory admin view
        public static void RegisterUserFromAccount(
            string fullName,
            string email,
            string role,
            string? clinicianName)
        {
            if (string.IsNullOrWhiteSpace(fullName) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(role))
            {
                return;
            }

            // avoid duplicates by email
            if (_users.Any(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))
                return;

            var user = new AdminUserSummary
            {
                Id = _nextUserId++,
                FullName = fullName.Trim(),
                Email = email.Trim(),
                Role = role.Trim(),
                IsAvailable = role == "Clinician",
                IsActive = true,
                AssignedClinicianName = role == "Patient" ? clinicianName?.Trim() : null
            };

            _users.Add(user);

            if (role == "Patient" && !string.IsNullOrWhiteSpace(clinicianName))
            {
                _patientClinicianMap[fullName.Trim()] = clinicianName.Trim();
            }
        }

        // Helper for AccountController: clinician dropdown during registration
        public static List<string> GetClinicianNames()
        {
            return _users
                .Where(u => u.Role == "Clinician" && u.IsActive)
                .Select(u => u.FullName)
                .OrderBy(n => n)
                .ToList();
        }

        // ------------- CSV / SENSOR HELPERS -------------

        // latest session ID for this patient
        private string? GetSessionIdForPatient(string patientName)
        {
            if (_patientSessionMap.TryGetValue(patientName, out var list) && list.Any())
                return list.Last(); // use latest day

            return _csvService.GetSessionIds().FirstOrDefault();
        }

        // Build a 0–100 trend line from frames (downsampled)
        private static List<int> BuildPressureTrendFromFrames(List<PressureFrameRow> frames)
        {
            var result = new List<int>();
            if (frames == null || frames.Count == 0)
                return result;

            const int maxPoints = 50;
            int step = Math.Max(1, frames.Count / maxPoints);

            for (int i = 0; i < frames.Count; i += step)
            {
                var f = frames[i];
                if (f.Values == null || f.Values.Length == 0) continue;

                var avg = f.Values.Average();
                var scaled = (int)Math.Clamp(avg, 0, 100);
                result.Add(scaled);
            }

            return result;
        }

        private async Task<(int? peak, double? avg)> GetLatestFrameStatsForPatientAsync(string patientName)
        {
            var sessionId = GetSessionIdForPatient(patientName);
            if (string.IsNullOrWhiteSpace(sessionId))
                return (null, null);

            var frames = await _csvService.LoadSessionAsync(sessionId);
            var latest = frames
                .LastOrDefault(f => f.Values != null && f.Values.Any(v => v > 0))
                ?? frames.LastOrDefault();

            if (latest == null || latest.Values == null || latest.Values.Length == 0)
                return (null, null);

            var peak = latest.Values.Max();
            var avg = latest.Values.Average();
            return (peak, avg);
        }

        // ------------- PATIENT DASHBOARD -------------

        public async Task<IActionResult> Patient()
        {
            // Only actual patients should see this view
            if (!IsPatient)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var patientName = HttpContext.Session.GetString("UserName") ?? "Patient";
            var today = DateTime.Today;

            _patientClinicianMap.TryGetValue(patientName, out var assignedClinician);

            var upcomingAppointments = _appointments
                .Where(a => a.PatientName == patientName && a.StartTime >= today)
                .OrderBy(a => a.StartTime)
                .ToList();

            var prescriptions = _prescriptions
                .Where(p => p.PatientName == patientName)
                .OrderByDescending(p => p.CreatedAt)
                .Take(20)
                .ToList();

            var activeAlerts = _alerts
                .Where(a => a.PatientName == patientName && !a.IsResolved)
                .OrderByDescending(a => a.RaisedAt)
                .ToList();

            var recentAlerts = _alerts
                .Where(a => a.PatientName == patientName)
                .OrderByDescending(a => a.RaisedAt)
                .Take(10)
                .ToList();

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

            var myFeedback = _feedbacks
                .Where(f => f.PatientName == patientName)
                .OrderByDescending(f => f.CreatedAt)
                .Take(10)
                .ToList();

            var vm = new PatientViewModel
            {
                PatientName = patientName,
                AssignedClinicianName = assignedClinician,

                UpcomingAppointments = upcomingAppointments,
                Prescriptions = prescriptions,
                ActiveAlerts = activeAlerts,
                RecentAlerts = recentAlerts,
                PressureRiskScore = score,
                PressureRiskLabel = riskLabel,
                PressureTrend = new List<int>(),
                MyFeedback = myFeedback
            };

            // CSV integration: snapshot + trend
            var sessionId = GetSessionIdForPatient(patientName);
            if (!string.IsNullOrWhiteSpace(sessionId))
            {
                var frames = await _csvService.LoadSessionAsync(sessionId);

                var latest = frames
                    .LastOrDefault(f => f.Values != null && f.Values.Any(v => v > 0))
                    ?? frames.LastOrDefault();

                if (latest != null && latest.Values != null && latest.Values.Length > 0)
                {
                    vm.SensorSessionId = sessionId;
                    vm.LatestFrameIndex = latest.FrameIndex;
                    vm.LatestFrameValues = latest.Values;
                    vm.LatestFrameMax = latest.Values.Max();
                    vm.LatestFrameAverage = latest.Values.Average();
                }

                vm.PressureTrend = BuildPressureTrendFromFrames(frames);
            }

            ViewData["Title"] = "Patient Dashboard";
            return View(vm);
        }

        // Patient books their own appointment with assigned clinician
        [HttpPost]
        public IActionResult BookAppointmentAsPatient(DateTime startTime, DateTime endTime)
        {
            if (!IsPatient)
                return RedirectToAction("AccessDenied", "Account");

            var patientName = HttpContext.Session.GetString("UserName") ?? "Patient";

            if (!_patientClinicianMap.TryGetValue(patientName, out var clinicianName) ||
                string.IsNullOrWhiteSpace(clinicianName))
            {
                TempData["PatientMessage"] =
                    "You don't have an assigned clinician yet. Please contact admin or your clinic.";
                return RedirectToAction("Patient");
            }

            if (startTime == default || endTime == default || endTime <= startTime)
            {
                TempData["PatientMessage"] = "Please choose a valid start and end time.";
                return RedirectToAction("Patient");
            }

            var appt = new AppointmentItem
            {
                Id = _nextAppointmentId++,
                PatientName = patientName,
                ClinicianName = clinicianName,
                StartTime = startTime,
                EndTime = endTime,
                Status = "Scheduled"
            };

            _appointments.Add(appt);
            LogAdmin($"Patient '{patientName}' booked appointment with {clinicianName} on {startTime:dd MMM HH:mm}.");
            TempData["PatientMessage"] =
                $"Appointment booked with {clinicianName} on {startTime:dd MMM HH:mm}.";

            return RedirectToAction("Patient");
        }

        // ------------- CLINICIAN DASHBOARD -------------

        public async Task<IActionResult> Clinician(string? selectedClinician)
        {
            if (!(IsClinician || IsAdmin))
                return RedirectToAction("AccessDenied", "Account");

            var clinicianNames = _users
                .Where(u => u.Role == "Clinician" && u.IsActive)
                .Select(u => u.FullName)
                .OrderBy(n => n)
                .ToList();

            // pick which clinician to show
            string effectiveClinician;
            if (!string.IsNullOrWhiteSpace(selectedClinician) && clinicianNames.Contains(selectedClinician))
            {
                effectiveClinician = selectedClinician;
            }
            else
            {
                var sessionName = HttpContext.Session.GetString("UserName");
                if (!string.IsNullOrWhiteSpace(sessionName) && clinicianNames.Contains(sessionName))
                    effectiveClinician = sessionName;
                else
                    effectiveClinician = clinicianNames.FirstOrDefault() ?? "Clinician";
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

            // patients from appointments + patients assigned to this clinician
            var patientFromAppointments = todayAppointments
                .Concat(upcomingAppointments)
                .Select(a => a.PatientName);

            var patientFromAssignments = _patientClinicianMap
                .Where(kvp => kvp.Value == effectiveClinician)
                .Select(kvp => kvp.Key);

            var patientNames = patientFromAppointments
                .Concat(patientFromAssignments)
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
                .Take(20)
                .ToList();

            var sensorSnapshots = new List<ClinicianViewModel.PatientSensorSnapshot>();
            foreach (var pName in patientNames)
            {
                var (peak, avg) = await GetLatestFrameStatsForPatientAsync(pName);
                sensorSnapshots.Add(new ClinicianViewModel.PatientSensorSnapshot
                {
                    PatientName = pName,
                    LatestPeak = peak,
                    LatestAverage = avg
                });
            }

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
                Prescriptions = myPrescriptions,
                PatientSensorSnapshots = sensorSnapshots
            };

            ViewData["Title"] = "Clinician Dashboard";
            return View(vm);
        }

        // ------------- ADMIN DASHBOARD -------------

        [HttpGet]
        public IActionResult Admin(string? search, string? roleFilter, bool showInactive = false)
        {
            if (!IsAdmin)
                return RedirectToAction("AccessDenied", "Account");

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

        // ------------- ADMIN / CLINICIAN ACTIONS -------------

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
                _patientClinicianMap.Remove(patient.FullName);

                LogAdmin($"Deleted patient '{patient.FullName}' and related data.");
                TempData["AdminMessage"] = $"Patient '{patient.FullName}' has been removed.";
            }

            return RedirectToAction("Admin");
        }

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

            TempData["ClinicianMessage"] = $"Appointment scheduled for {patientName}.";
            return RedirectToAction("Clinician", new { selectedClinician = clinicianName });
        }

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

                TempData["ClinicianMessage"] = $"Alert for {alert.PatientName} has been marked as resolved.";
                return RedirectToAction("Clinician");
            }

            return IsAdmin ? RedirectToAction("Admin") : RedirectToAction("Clinician");
        }

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

                TempData["ClinicianMessage"] = "Please fill in all alert details.";
                return RedirectToAction("Clinician");
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

            TempData["ClinicianMessage"] = $"Alert created for {patientName} ({severity}).";
            return RedirectToAction("Clinician");
        }

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
                TempData["ClinicianMessage"] = "Please provide patient, medication and dosage.";
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
                TempData["PatientMessage"] = "Please select clinician, rating (1–5) and enter a comment.";
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
