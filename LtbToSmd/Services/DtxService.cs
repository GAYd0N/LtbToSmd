using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using LtbToSmd.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Tga;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Quantization;

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
            => Convert(inputPath, outputDir, DtxOutputFormat.Png);

        public string Convert(string inputPath, string outputDir, DtxOutputFormat format,
                              bool indexedBmp = false, int maxEdgeLength = 0,
                              CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

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

            ct.ThrowIfCancellationRequested();

            var ext = GetExtension(format);
            var fileName = Path.GetFileNameWithoutExtension(inputPath);
            var outputPath = Path.Combine(outputDir, fileName + ext);

            // 加载图像
            using var image = Image.LoadPixelData<Rgba32>(dtx.RgbaPixels, dtx.Width, dtx.Height);

            ct.ThrowIfCancellationRequested();

            // 等比缩放
            if (maxEdgeLength > 0)
                ScaleToFit(image, maxEdgeLength);

            // 保存
            SaveImage(image, outputPath, format, indexedBmp);

            _logger?.PrintLog($"[DTX2PNG] {fileName}.dtx → {fileName}{ext} ({dtx.Width}x{dtx.Height})");
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

        // ───────────── 格式映射 ─────────────

        private static string GetExtension(DtxOutputFormat format) => format switch
        {
            DtxOutputFormat.Bmp => ".bmp",
            DtxOutputFormat.Tga => ".tga",
            _ => ".png"
        };

        // ───────────── 等比缩放 ─────────────

        private static void ScaleToFit(Image<Rgba32> image, int maxEdge)
        {
            int w = image.Width;
            int h = image.Height;
            int longest = Math.Max(w, h);
            if (longest <= maxEdge) return;

            double ratio = (double)maxEdge / longest;
            int newW = (int)(w * ratio);
            int newH = (int)(h * ratio);
            if (newW < 1) newW = 1;
            if (newH < 1) newH = 1;

            image.Mutate(ctx => ctx.Resize(newW, newH, KnownResamplers.Lanczos3));
        }

        // ───────────── 保存 ─────────────

        private static void SaveImage(Image<Rgba32> image, string outputPath,
                                       DtxOutputFormat format, bool indexedBmp)
        {
            switch (format)
            {
                case DtxOutputFormat.Png:
                    image.Save(outputPath, new PngEncoder());
                    break;

                case DtxOutputFormat.Bmp:
                    if (indexedBmp)
                    {
                        // 量化到 256 色以内后保存为 8-bit BMP
                        image.Mutate(ctx => ctx.Quantize(new WuQuantizer()));
                        image.Save(outputPath, new BmpEncoder { BitsPerPixel = BmpBitsPerPixel.Pixel8 });
                    }
                    else
                    {
                        image.Save(outputPath, new BmpEncoder { BitsPerPixel = BmpBitsPerPixel.Pixel32 });
                    }
                    break;

                case DtxOutputFormat.Tga:
                    image.Save(outputPath, new TgaEncoder { BitsPerPixel = TgaBitsPerPixel.Pixel32 });
                    break;
            }
        }
    }
}
