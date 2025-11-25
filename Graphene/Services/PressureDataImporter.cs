using System.Globalization;
using Graphene_Group_Project.Data;
using Graphene_Group_Project.Data.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

// 2

namespace Graphene_Group_Project.Services
{
    public interface IPressureDataImporter
    {
        /// <summary>
        /// Imports all CSV frames from a dataset folder into the DB
        /// and generates alerts. Returns the number of frames imported.
        /// </summary>
        Task<int> ImportDatasetAsync(int patientId, string datasetFolderName, CancellationToken cancellationToken = default);
    }

    public class PressureDataImporter : IPressureDataImporter
    {
        private readonly AppDbContext _db;
        private readonly ILogger<PressureDataImporter> _logger;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;

        public PressureDataImporter(
            AppDbContext db,
            ILogger<PressureDataImporter> logger,
            IWebHostEnvironment env,
            IConfiguration config)
        {
            _db = db;
            _logger = logger;
            _env = env;
            _config = config;
        }

        public async Task<int> ImportDatasetAsync(int patientId, string datasetFolderName, CancellationToken cancellationToken = default)
        {
            // 1) Resolve base folder from config
            var configuredBase = _config["PressureData:BaseFolder"];
            var baseFolder = string.IsNullOrWhiteSpace(configuredBase)
                ? Path.Combine(_env.ContentRootPath, "DataFiles", "PressureCsv")
                : Path.IsPathRooted(configuredBase)
                    ? configuredBase
                    : Path.Combine(_env.ContentRootPath, configuredBase);

            var datasetPath = Path.Combine(baseFolder, datasetFolderName);

            if (!Directory.Exists(datasetPath))
            {
                throw new DirectoryNotFoundException($"Dataset folder not found: {datasetPath}");
            }

            // 2) Ensure patient exists
            var patient = await _db.Patients
                .FirstOrDefaultAsync(p => p.PatientId == patientId, cancellationToken);

            if (patient == null)
            {
                throw new InvalidOperationException($"No patient with ID {patientId}.");
            }

            // 3) Create a DataFile entry for this dataset
            var dataFile = new DataFile
            {
                PatientId = patientId,
                FilePath = datasetPath,
                ImportedUtc = DateTime.UtcNow
            };

            _db.DataFiles.Add(dataFile);
            await _db.SaveChangesAsync(cancellationToken); // get FileId

            // 4) Read all CSV files as frames
            var csvFiles = Directory
                .GetFiles(datasetPath, "*.csv", SearchOption.TopDirectoryOnly)
                .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (csvFiles.Count == 0)
            {
                _logger.LogWarning("No CSV files found in dataset folder {Folder}", datasetPath);
                return 0;
            }

            // Thresholds
            var lowThr = GetIntConfig("PressureData:AlertThresholdLow", 40);
            var medThr = GetIntConfig("PressureData:AlertThresholdMedium", 60);
            var highThr = GetIntConfig("PressureData:AlertThresholdHigh", 80);
            var pixelThr = GetIntConfig("PressureData:PixelThreshold", 50);

            var frameRate = GetIntConfig("PressureData:FrameRate", 15); // frames/sec
            var frameDuration = TimeSpan.FromSeconds(1.0 / frameRate);
            var startTimeUtc = DateTime.UtcNow;

            var frameIndex = 0;

            foreach (var file in csvFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var (width, height, values) = await ReadMatrixAsync(file, cancellationToken);

                var maxPressure = values.Max();
                var pixelsAboveThr = values.Count(v => v >= pixelThr);
                var totalPixels = width * height;
                decimal? contactAreaPct = totalPixels > 0
                    ? (decimal)pixelsAboveThr * 100m / totalPixels
                    : null;

                var capturedUtc = startTimeUtc + frameDuration * frameIndex;

                var frame = new PressureFrame
                {
                    PatientId = patientId,
                    FileId = dataFile.FileId,
                    FrameIndex = frameIndex,
                    CapturedUtc = capturedUtc,
                    Width = (byte)width,
                    Height = (byte)height,
                    PeakPressure = maxPressure,
                    PixelsAboveThr = pixelsAboveThr,
                    ContactAreaPct = contactAreaPct
                };

                _db.PressureFrames.Add(frame);

                // Decide if this frame should generate an Alert
                byte severity = 0;
                if (maxPressure >= highThr)
                    severity = 3;
                else if (maxPressure >= medThr)
                    severity = 2;
                else if (maxPressure >= lowThr)
                    severity = 1;

                if (severity > 0)
                {
                    var alert = new Alert
                    {
                        PatientId = patientId,
                        Frame = frame, // EF will set FrameId
                        TriggeredUtc = capturedUtc,
                        Severity = severity,
                        MaxPressure = maxPressure,
                        PixelsAboveThr = pixelsAboveThr,
                        RegionJson = null,
                        Status = 0, // 0 = new
                        Notes = $"Auto-generated from dataset '{datasetFolderName}' (frame {frameIndex})."
                    };

                    _db.Alerts.Add(alert);
                }

                frameIndex++;
            }

            dataFile.FirstTimestampUtc = startTimeUtc;
            dataFile.LastTimestampUtc = startTimeUtc + frameDuration * (frameIndex - 1);

            await _db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Imported {FrameCount} frames for patient {PatientId} from {DatasetFolder}.",
                frameIndex, patientId, datasetFolderName);

            return frameIndex;
        }

        private int GetIntConfig(string key, int defaultValue)
        {
            var value = _config[key];
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
            {
                return result;
            }
            return defaultValue;
        }

        /// <summary>
        /// Reads a CSV file that represents a single frame.
        /// Each line = one row; values separated by comma/semicolon/tab.
        /// </summary>
        private async Task<(int width, int height, List<int> values)> ReadMatrixAsync(
            string path,
            CancellationToken cancellationToken)
        {
            var lines = await File.ReadAllLinesAsync(path, cancellationToken);
            var cleaned = lines
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .ToArray();

            if (cleaned.Length == 0)
                throw new InvalidDataException($"CSV file {path} is empty.");

            var firstParts = cleaned[0]
                .Split(new[] { ',', ';', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            var width = firstParts.Length;
            var height = cleaned.Length;

            var values = new List<int>(width * height);

            foreach (var line in cleaned)
            {
                var parts = line
                    .Split(new[] { ',', ';', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length != width)
                {
                    throw new InvalidDataException(
                        $"Inconsistent column count in {path}. Expected {width}, got {parts.Length}.");
                }

                foreach (var p in parts)
                {
                    if (!int.TryParse(p, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v))
                    {
                        throw new InvalidDataException($"Non-numeric value '{p}' in {path}.");
                    }
                    values.Add(v);
                }
            }

            return (width, height, values);
        }
    }
}
