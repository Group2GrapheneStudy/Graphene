using Microsoft.AspNetCore.Mvc;

namespace GrapheneTrace.Web.Controllers  // <- change to your namespace if needed
{
    public class DashboardController : Controller
    {
        // GET: /Dashboard/Patient
        public IActionResult Patient()
        {
            ViewData["Title"] = "Patient Dashboard";
            return View();
        }

        // GET: /Dashboard/Clinician
        public IActionResult Clinician()
        {
            ViewData["Title"] = "Clinician Dashboard";
            return View();
        }

        // GET: /Dashboard/Admin
        public IActionResult Admin()
        {
            ViewData["Title"] = "Admin Dashboard";
            return View();
        }
    }
}
