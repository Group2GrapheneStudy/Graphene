using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

namespace Graphene_Group_Project.Controllers
{
    public class HomeController : Controller
    {
        // GET: / or /Home/Index
        [HttpGet]
        public IActionResult Index()
        {
            // High-level system summary (dummy values for now – replace with real stats later)
            ViewBag.TotalPatients = 12;
            ViewBag.TotalClinicians = 4;
            ViewBag.TotalAdmins = 2;
            ViewBag.TotalAlerts = 7;

            // Quick navigation “tiles” for each role
            ViewBag.RoleTiles = new[]
            {
                new {
                    Title       = "Patient Portal",
                    Description = "View your pressure heatmaps, alerts and comments.",
                    ActionText  = "Go to Patient Dashboard",
                    Url         = "/Patient/Dashboard",
                    Badge       = "Patients"
                },
                new {
                    Title       = "Clinician Portal",
                    Description = "Review patient data, alerts and generate reports.",
                    ActionText  = "Go to Clinician Dashboard",
                    Url         = "/Clinician/Dashboard",
                    Badge       = "Clinicians"
                },
                new {
                    Title       = "Admin Portal",
                    Description = "Manage user accounts, roles and system health.",
                    ActionText  = "Go to Admin Dashboard",
                    Url         = "/Admin/Dashboard",
                    Badge       = "Admins"
                }
            };

            // Simple recent activity / notifications list
            ViewBag.Notifications = new[]
            {
                new { Time = DateTime.Now.AddMinutes(-10).ToString("HH:mm"), Message = "High pressure alert triggered for Alice Patient." },
                new { Time = DateTime.Now.AddHours(-1).ToString("HH:mm"),  Message = "Clinician Bob generated a daily report for Dana Patient." },
                new { Time = DateTime.Now.AddHours(-3).ToString("HH:mm"),  Message = "New patient account created by Admin Charlie." }
            };

            return View();
        }
    }
}
