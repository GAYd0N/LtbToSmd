using System;
using System.IO;
using BCnEncoder.Decoder;
using BCnEncoder.ImageSharp;
using BCnEncoder.Shared;

namespace LtbToSmd.Models
{
    /// <summary>
    /// DTX 文件解析与解码模型。
    /// 基于 dtx2png 项目的 Dtx 类实现，支持多种 LithTech DTX 版本与像素格式。
    /// </summary>
    public class DtxModel
    {
        // ---- 版本常量 ----
        private const int DTX_VERSION_LT1 = -2;
        private const int DTX_VERSION_LT15 = -3;
        private const int DTX_VERSION_LT2 = -5;
        private const int DTX_COMMANDSTRING_LENGTH = 128;

        // ---- BPP（BytesPerPixel）常量 ----
        private const int BPP_8_P = 0;      // 8-bit 调色板
        private const int BPP_32 = 3;        // 32-bit 未压缩
        private const int BPP_DXT1 = 4;      // DXT1 / BC1
        private const int BPP_DXT3 = 5;      // DXT3 / BC2
        private const int BPP_DXT5 = 6;      // DXT5 / BC3
        private const int BPP_32_P = 7;      // 32-bit 调色板（暂未实现）

        // ---- 解析结果 ----
        public int Width { get; private set; }
        public int Height { get; private set; }
        public byte[] RgbaPixels { get; private set; } = Array.Empty<byte>();
        public string Error { get; private set; } = string.Empty;
        public bool HasError => !string.IsNullOrEmpty(Error);

        /// <summary>
        /// 从原始字节流解析 DTX 文件，解码为 RGBA 像素数据。
        /// </summary>
        public bool Load(byte[] fileData)
        {
            try
            {
                using var ms = new MemoryStream(fileData);
                using var reader = new BinaryReader(ms);
                return Read(reader);
            }
            catch (Exception ex)
            {
                Error = $"解析 DTX 时异常: {ex.Message}";
                return false;
            }
        }

        // ═══════════════════════════ 头部解析 ═══════════════════════════

        private bool Read(BinaryReader reader)
        {
            int resourceType = reader.ReadInt32();

            // 某些文件没有 ResourceType 字段，从 0 开始就是 Version
            if (resourceType != 0)
                reader.BaseStream.Seek(0, SeekOrigin.Begin);

            int version = (int)reader.ReadUInt32();

            if (version != DTX_VERSION_LT1 && version != DTX_VERSION_LT15 && version != DTX_VERSION_LT2)
            {
                Error = $"不支持的 DTX 版本: {version} (期望 -2/-3/-5)";
                return false;
            }

            Width = reader.ReadUInt16();
            Height = reader.ReadUInt16();
            int mipmapCount = reader.ReadUInt16();
            /* sectionCount = */ reader.ReadUInt16();
            uint flags = reader.ReadUInt32();
            /* userFlags = */ reader.ReadUInt32();
            int textureGroup = reader.ReadByte();
            /* mipmapsToUse = */ reader.ReadByte();
            int bytesPerPixel = reader.ReadByte();
            /* mipmapOffset = */ reader.ReadByte();
            /* mipmapTextureCoordOffset = */ reader.ReadByte();
            /* texturePriority = */ reader.ReadByte();
            /* detailTextureScale = */ reader.ReadSingle();
            /* detailTextureAngle = */ reader.ReadUInt16();

            // LT1.5 / LT2 有 128 字节的命令字符串
            if (version == DTX_VERSION_LT15 || version == DTX_VERSION_LT2)
                reader.ReadBytes(DTX_COMMANDSTRING_LENGTH);

            // 格式判定
            var format = DetectFormat(flags, textureGroup);

            return ReadTextureData(reader, format, mipmapCount, bytesPerPixel);
        }

        private enum TextureFormat { Unknown, BGRA, RGBA, DXT1, DXT5 };

        private static TextureFormat DetectFormat(uint flags, int textureGroup)
        {
            return flags switch
            {
                136 => TextureFormat.RGBA,
                8 => textureGroup switch
                {
                    0 => TextureFormat.BGRA,
                    1 => TextureFormat.DXT1,
                    _ => TextureFormat.Unknown
                },
                _ => TextureFormat.Unknown
            };
        }

        // ═══════════════════════════ 纹理数据解析 ═══════════════════════════

        private bool ReadTextureData(BinaryReader reader, TextureFormat format, int mipmapCount, int bytesPerPixel)
        {
            // 8-bit 调色板（LT1 / LT1.5 / BPP == 0）
            if (format == TextureFormat.Unknown && bytesPerPixel == BPP_8_P)
            {
                RgbaPixels = Read8BitPalette(reader);
                return true;
            }

            // DXT1 / DXT3 / DXT5 压缩
            if (bytesPerPixel is BPP_DXT1 or BPP_DXT3 or BPP_DXT5)
            {
                RgbaPixels = ReadCompressedSection(reader, bytesPerPixel, mipmapCount);
                return RgbaPixels.Length > 0;
            }

            // 32-bit 未压缩
            if (bytesPerPixel == BPP_32)
            {
                var result = Read32BitTexture(reader, format);
                if (result == null) return false;
                RgbaPixels = result;
                return true;
            }

            // 32-bit 调色板（未实现）
            if (bytesPerPixel == BPP_32_P)
            {
                Error = "32-bit 调色板格式暂不支持";
                return false;
            }

            Error = $"未知的像素格式: BPP={bytesPerPixel}, Flags={format}";
            return false;
        }

        // ────────────────────────── 8-bit 调色板 ──────────────────────────

        private byte[] Read8BitPalette(BinaryReader reader)
        {
            reader.ReadUInt32(); // 跳过两个 uint
            reader.ReadUInt32();

            // 读取 256 色调色板（A, R, G, B × 256）
            var palette = new (byte r, byte g, byte b, byte a)[256];
            for (int i = 0; i < 256; i++)
            {
                byte a = reader.ReadByte();
                byte r = reader.ReadByte();
                byte g = reader.ReadByte();
                byte bVal = reader.ReadByte();
                palette[i] = (r, g, bVal, a);
            }

            // 读取索引数据
            int bufferSize = Width * Height;
            byte[] indices = reader.ReadBytes(bufferSize);

            // 通过调色板映射为 RGBA
            var rgba = new byte[bufferSize * 4];
            for (int i = 0; i < bufferSize; i++)
            {
                var c = palette[indices[i]];
                rgba[i * 4 + 0] = c.r;
                rgba[i * 4 + 1] = c.g;
                rgba[i * 4 + 2] = c.b;
                rgba[i * 4 + 3] = c.a;
            }
            return rgba;
        }

        // ────────────────────────── DXT 压缩 ──────────────────────────

        private byte[] ReadCompressedSection(BinaryReader reader, int bytesPerPixel, int mipmapCount)
        {
            byte[] result = Array.Empty<byte>();
            int originalWidth = Width;
            int originalHeight = Height;

            // 从当前位置读到文件末尾（支持多个纹理片）
            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                if (result.Length > 0) // 非第一个片，跳过 32 个未知字节
                    reader.BaseStream.Seek(32, SeekOrigin.Current);

                for (int i = 0; i < mipmapCount; i++)
                {
                    Width = UtilMath.DivideByPowerOfTwo(originalWidth, i);
                    Height = UtilMath.DivideByPowerOfTwo(originalHeight, i);

                    var pixels = ReadCompressedMip(reader, bytesPerPixel);
                    if (i == 0) result = pixels; // 只保留第一个 mipmap
                }
            }

            Width = originalWidth;
            Height = originalHeight;
            return result;
        }

        private byte[] ReadCompressedMip(BinaryReader reader, int bytesPerPixel)
        {
            var compressionFormat = CompressionFormat.Bc1; // DXT1
            int blockBytes = 8;

            if (bytesPerPixel == BPP_DXT3)
            {
                compressionFormat = CompressionFormat.Bc2; // DXT3
                blockBytes = 16;
            }
            else if (bytesPerPixel == BPP_DXT5)
            {
                compressionFormat = CompressionFormat.Bc3; // DXT5
                blockBytes = 16;
            }

            int blocksX = (Width + 3) / 4;
            int blocksY = (Height + 3) / 4;
            int length = blocksX * blocksY * blockBytes;

            byte[] compressedData = reader.ReadBytes(length);

            // BCnEncoder 解码
            var decoder = new BcDecoder();
            using var ms = new MemoryStream(compressedData);

            // DecodeRawToImageRgba32 是 BCnEncoder.Net.ImageSharp 的扩展方法
            // 返回 Image<Rgba32>，我们需要从中提取 RGBA 字节
            using var image = decoder.DecodeRawToImageRgba32(ms, Width, Height, compressionFormat);

            // 从 Image<Rgba32> 提取原始 RGBA 数据
            var rgba = new byte[Width * Height * 4];
            image.CopyPixelDataTo(rgba);

            // 验证尺寸匹配
            if (rgba.Length != Width * Height * 4)
            {
                Error = $"解码后像素数据尺寸不匹配: 期望 {Width * Height * 4}, 实际 {rgba.Length}";
                return Array.Empty<byte>();
            }

            return rgba;
        }

        // ────────────────────────── 32-bit 未压缩 ──────────────────────────

        private byte[]? Read32BitTexture(BinaryReader reader, TextureFormat format)
        {
            int size = Width * Height * 4;
            byte[] data = reader.ReadBytes(size);
            if (data.Length < size)
            {
                Error = $"数据不足: 需要 {size} 字节, 实际读取 {data.Length} 字节";
                return null;
            }

            var rgba = new byte[size];
            int idx = 0;
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (format == TextureFormat.BGRA)
                    {
                        byte b = data[idx++];
                        byte g = data[idx++];
                        byte r = data[idx++];
                        byte a = data[idx++];
                        rgba[(y * Width + x) * 4 + 0] = r;
                        rgba[(y * Width + x) * 4 + 1] = g;
                        rgba[(y * Width + x) * 4 + 2] = b;
                        rgba[(y * Width + x) * 4 + 3] = a;
                    }
                    else
                    {
                        // RGBA — 跳过 alpha（数据可能不正确）
                        rgba[(y * Width + x) * 4 + 0] = data[idx++];
                        rgba[(y * Width + x) * 4 + 1] = data[idx++];
                        rgba[(y * Width + x) * 4 + 2] = data[idx++];
                        idx++; // 跳过 alpha
                        rgba[(y * Width + x) * 4 + 3] = 0xFF;
                    }
                }
            }
            return rgba;
        }
    }

    /// <summary>
    /// 内部工具方法（与 dtx2png 的 Util 对应）
    /// </summary>
    internal static class UtilMath
    {
        public static int DivideByPowerOfTwo(int value, int power)
        {
            int ret = value >> power;
            return ret < 1 ? 1 : ret;
        }
    }
}
