using Graphene_Group_Project.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

// 2

namespace Graphene_Group_Project.Controllers
{
    public class DataImportController : Controller
    {
        private readonly IPressureDataImporter _importer;

        public DataImportController(IPressureDataImporter importer)
        {
            _importer = importer;
        }

        private bool IsAdmin =>
            HttpContext.Session.GetString("UserRole") == "Admin";

        [HttpPost]
        public async Task<IActionResult> Import(int patientId, string datasetFolder)
        {
            if (!IsAdmin)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            if (string.IsNullOrWhiteSpace(datasetFolder))
            {
                TempData["AdminMessage"] = "Please provide a dataset folder name.";
                return RedirectToAction("Admin", "Dashboard");
            }

            try
            {
                var frames = await _importer.ImportDatasetAsync(patientId, datasetFolder);
                TempData["AdminMessage"] = $"Imported {frames} frames for patient ID {patientId} from '{datasetFolder}'.";
            }
            catch (Exception ex)
            {
                TempData["AdminMessage"] = $"Import failed: {ex.Message}";
            }

            return RedirectToAction("Admin", "Dashboard");
        }
    }
}
