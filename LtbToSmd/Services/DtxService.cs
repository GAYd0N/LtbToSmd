using System;
using System.Collections.Generic;
using System.IO;
using LtbToSmd.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace LtbToSmd.Services
{
    public class DtxService : IDtxService
    {
        private readonly ILogger? _logger;

        public DtxService(ILogger? logger = null)
        {
            _logger = logger;
        }

        public string ConvertToPng(string inputPath, string outputDir)
        {
            if (!File.Exists(inputPath))
                throw new FileNotFoundException("DTX 文件未找到", inputPath);

            if (!Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);

            var fileData = File.ReadAllBytes(inputPath);
            var dtx = new DtxModel();
            if (!dtx.Load(fileData))
            {
                var msg = dtx.HasError ? dtx.Error : "未知解析错误";
                throw new InvalidOperationException($"DTX 解析失败: {msg}");
            }

            var fileName = Path.GetFileNameWithoutExtension(inputPath);
            var outputPath = Path.Combine(outputDir, fileName + ".png");

            // 使用 ImageSharp 保存为 PNG
            using var image = Image.LoadPixelData<Rgba32>(dtx.RgbaPixels, dtx.Width, dtx.Height);
            image.Save(outputPath, new PngEncoder());

            _logger?.PrintLog($"[DTX2PNG] {fileName}.dtx → {fileName}.png ({dtx.Width}x{dtx.Height})");
            return outputPath;
        }

        public IEnumerable<string> ConvertAllInDirectory(string inputDir, string outputDir)
        {
            var results = new List<string>();
            if (!Directory.Exists(inputDir))
                return results;

            foreach (var file in Directory.GetFiles(inputDir, "*.dtx"))
            {
                try
                {
                    results.Add(ConvertToPng(file, outputDir));
                }
                catch (Exception ex)
                {
                    _logger?.PrintLog($"[DTX2PNG] 转换失败: {Path.GetFileName(file)} — {ex.Message}");
                }
            }
            return results;
        }
    }
}
