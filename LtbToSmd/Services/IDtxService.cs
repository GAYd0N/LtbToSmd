using System.Collections.Generic;
using System.Threading;

namespace LtbToSmd.Services
{
    public enum DtxOutputFormat
    {
        Png,
        Bmp,
        Tga
    }

    public interface IDtxService
    {
        /// <summary>
        /// 将单个 DTX 文件转换为 PNG，返回文件路径。
        /// </summary>
        string ConvertToPng(string inputPath, string outputDir);

        /// <summary>
        /// 将单个 DTX 文件转换为指定格式，支持缩放和索引色选项。
        /// </summary>
        string Convert(string inputPath, string outputDir, DtxOutputFormat format,
                       bool indexedBmp = false, int maxEdgeLength = 0,
                       CancellationToken ct = default);

        /// <summary>
        /// 将目录下所有 DTX 文件转换为 PNG。
        /// </summary>
        IEnumerable<string> ConvertAllInDirectory(string inputDir, string outputDir);
    }
}
