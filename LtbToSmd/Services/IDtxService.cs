using System.Collections.Generic;

namespace LtbToSmd.Services
{
    public interface IDtxService
    {
        /// <summary>
        /// 将单个 DTX 文件转换为 PNG，返回 PNG 文件路径。
        /// </summary>
        string ConvertToPng(string inputPath, string outputDir);

        /// <summary>
        /// 将目录下所有 DTX 文件转换为 PNG。
        /// </summary>
        IEnumerable<string> ConvertAllInDirectory(string inputDir, string outputDir);
    }
}
