using System.Linq;
using System.Threading.Tasks;
using GrapheneTrace.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace GrapheneTrace.Web.Controllers
{
    public class SensorDataController : Controller
    {
        private readonly PressureCsvService _csvService;

        public SensorDataController(PressureCsvService csvService)
        {
            _csvService = csvService;
        }

        // GET: /SensorData
        public IActionResult Index()
        {
            var sessions = _csvService.GetSessionIds().ToList();
            return View(sessions);
        }

        // GET: /SensorData/Session?id=71e66ab3_20251012
        public async Task<IActionResult> Session(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return RedirectToAction(nameof(Index));

            var frames = await _csvService.LoadSessionAsync(id);

            if (!frames.Any())
            {
                ViewBag.Message = "No data found for this session.";
            }

            return View(frames);
        }
    }
}
