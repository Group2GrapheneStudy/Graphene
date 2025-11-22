using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace GrapheneTrace.Web.Controllers
{
    public class DashboardController : Controller
    {
        // ----------------------------------------------------------
        // ROLE HELPERS
        // ----------------------------------------------------------
        private string? UserRole =>
            HttpContext.Session.GetString("UserRole");

        private bool IsAdmin =>
            UserRole == "Admin";

        private bool IsClinician =>
            UserRole == "Clinician";

        private bool IsPatient =>
            UserRole == "Patient";

        // ----------------------------------------------------------
        // PATIENT DASHBOARD
        // Allowed Roles: Patient, Clinician, Admin
        // ----------------------------------------------------------
        public IActionResult Patient()
        {
            if (!(IsPatient || IsClinician || IsAdmin))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            ViewData["Title"] = "Patient Dashboard";
            return View();
        }

        // ----------------------------------------------------------
        // CLINICIAN DASHBOARD
        // Allowed Roles: Clinician, Admin
        // ----------------------------------------------------------
        public IActionResult Clinician()
        {
            if (!(IsClinician || IsAdmin))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            ViewData["Title"] = "Clinician Dashboard";
            return View();
        }

        // ----------------------------------------------------------
        // ADMIN DASHBOARD
        // Allowed Roles: Admin ONLY
        // ----------------------------------------------------------
        public IActionResult Admin()
        {
            if (!IsAdmin)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            ViewData["Title"] = "Admin Dashboard";
            return View();
        }
    }
}
