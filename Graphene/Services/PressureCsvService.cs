using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;

namespace GrapheneTrace.Web.Services
{
    // One row from a CSV: a single frame of the pressure map
    public class PressureFrameRow
    {
        public int FrameIndex { get; set; }
        public int[] Values { get; set; } = Array.Empty<int>();
    }

    // Service that loads CSV files from wwwroot/data/GTLB-Data
    public class PressureCsvService
    {
        private readonly IWebHostEnvironment _env;
        private readonly string _dataFolder;

        public PressureCsvService(IWebHostEnvironment env)
        {
            _env = env;
            _dataFolder = Path.Combine(_env.WebRootPath ?? "wwwroot", "data", "GTLB-Data");

            // Make sure folder exists so we don't crash even if it's missing
            Directory.CreateDirectory(_dataFolder);
        }

        // Returns IDs of all sessions (file names without .csv)
        public IEnumerable<string> GetSessionIds()
        {
            if (!Directory.Exists(_dataFolder))
                return Enumerable.Empty<string>();

            return Directory
                .GetFiles(_dataFolder, "*.csv")
                .Select(f => Path.GetFileNameWithoutExtension(f) ?? "")
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .OrderBy(id => id);
        }

        // Load one CSV (by ID) into a list of frames
        public async Task<List<PressureFrameRow>> LoadSessionAsync(string sessionId)
        {
            var fileName = sessionId.EndsWith(".csv", StringComparison.OrdinalIgnoreCase)
                ? sessionId
                : sessionId + ".csv";

            var fullPath = Path.Combine(_dataFolder, fileName);

            if (!System.IO.File.Exists(fullPath))
                return new List<PressureFrameRow>();

            var frames = new List<PressureFrameRow>();

            using var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new StreamReader(stream);

            int frameIndex = 0;

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var parts = line.Split(',');
                var values = new int[parts.Length];

                for (int i = 0; i < parts.Length; i++)
                {
                    int.TryParse(parts[i], out values[i]); // if parse fails, value stays 0
                }

                frames.Add(new PressureFrameRow
                {
                    FrameIndex = frameIndex++,
                    Values = values
                });
            }

            return frames;
        }
    }
}
