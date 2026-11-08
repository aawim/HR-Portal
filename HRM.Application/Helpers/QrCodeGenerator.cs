using System;
using System.IO;
using System.Text;

namespace HRM.Helpers;

/// <summary>
/// Lightweight, self-contained QR Code generator implementing ISO/IEC 18004 Byte-mode.
/// Generates vector SVG and pure BMP byte arrays without native OS or third-party dependencies.
/// </summary>
public static class QrCodeGenerator
{
    public static bool[,] GenerateMatrix(string content)
    {
        if (string.IsNullOrEmpty(content)) content = "https://hrm.gov.mv";
        byte[] data = Encoding.UTF8.GetBytes(content);

        // Select minimal QR version (Version 1..6) with Error Correction Level M
        // Version capacities for Level M (Byte mode):
        // V1: 14, V2: 26, V3: 42, V4: 62, V5: 84, V6: 106, V7: 122, V8: 152
        int version = 1;
        int[] capacities = [0, 14, 26, 42, 62, 84, 106, 122, 152];
        for (int v = 1; v < capacities.Length; v++)
        {
            if (data.Length <= capacities[v])
            {
                version = v;
                break;
            }
            version = v;
        }

        int size = 17 + 4 * version;
        bool[,] modules = new bool[size, size];
        bool[,] isFunction = new bool[size, size];

        // 1. Finder patterns (Top-Left, Top-Right, Bottom-Left)
        DrawFinderPattern(modules, isFunction, 0, 0, size);
        DrawFinderPattern(modules, isFunction, size - 7, 0, size);
        DrawFinderPattern(modules, isFunction, 0, size - 7, size);

        // 2. Alignment patterns for Version >= 2
        if (version >= 2)
        {
            int alignPos = version switch
            {
                2 => 18,
                3 => 22,
                4 => 26,
                5 => 30,
                6 => 34,
                7 => 22, // Primary pos
                _ => size - 7
            };
            DrawAlignmentPattern(modules, isFunction, alignPos, alignPos);
        }

        // 3. Timing patterns
        for (int i = 8; i < size - 8; i++)
        {
            bool val = i % 2 == 0;
            if (!isFunction[6, i]) { modules[6, i] = val; isFunction[6, i] = true; }
            if (!isFunction[i, 6]) { modules[i, 6] = val; isFunction[i, 6] = true; }
        }

        // 4. Dark module
        modules[4 * version + 9, 8] = true;
        isFunction[4 * version + 9, 8] = true;

        // 5. Reserve format bits areas
        for (int i = 0; i <= 8; i++)
        {
            if (i != 6) { isFunction[8, i] = true; isFunction[i, 8] = true; }
        }
        for (int i = size - 8; i < size; i++)
        {
            isFunction[8, i] = true;
            isFunction[i, 8] = true;
        }

        // 6. Data bitstream encoding
        byte[] codewords = EncodeCodewords(data, version);

        // 7. Place codewords in zigzag upward/downward pattern
        int byteIndex = 0;
        int bitIndex = 7;
        bool goingUp = true;

        for (int right = size - 1; right > 0; right -= 2)
        {
            if (right == 6) right--; // Skip vertical timing column

            for (int vert = 0; vert < size; vert++)
            {
                int y = goingUp ? size - 1 - vert : vert;
                for (int x = right; x >= right - 1; x--)
                {
                    if (isFunction[y, x]) continue;

                    bool bit = false;
                    if (byteIndex < codewords.Length)
                    {
                        bit = ((codewords[byteIndex] >> bitIndex) & 1) == 1;
                        bitIndex--;
                        if (bitIndex < 0)
                        {
                            bitIndex = 7;
                            byteIndex++;
                        }
                    }

                    // Apply Mask Pattern 0: (row + col) % 2 == 0
                    bool mask = (y + x) % 2 == 0;
                    modules[y, x] = bit ^ mask;
                }
            }
            goingUp = !goingUp;
        }

        // 8. Format bits: Mask 000 with Error Correction Level M (00) -> Format bits 101010000010010
        // (Precomputed standard format string for Level M, mask 0)
        int formatBits = 0x5412 ^ 0x0000; // 0x5412
        WriteFormatBits(modules, formatBits, size);

        return modules;
    }

    private static void DrawFinderPattern(bool[,] modules, bool[,] isFunction, int x, int y, int size)
    {
        for (int r = -1; r <= 7; r++)
        {
            for (int c = -1; c <= 7; c++)
            {
                int py = y + r;
                int px = x + c;
                if (px < 0 || px >= size || py < 0 || py >= size) continue;

                isFunction[py, px] = true;
                if (r == -1 || r == 7 || c == -1 || c == 7)
                    modules[py, px] = false;
                else if (r == 0 || r == 6 || c == 0 || c == 6)
                    modules[py, px] = true;
                else if (r >= 2 && r <= 4 && c >= 2 && c <= 4)
                    modules[py, px] = true;
                else
                    modules[py, px] = false;
            }
        }
    }

    private static void DrawAlignmentPattern(bool[,] modules, bool[,] isFunction, int centerX, int centerY)
    {
        for (int r = -2; r <= 2; r++)
        {
            for (int c = -2; c <= 2; c++)
            {
                int py = centerY + r;
                int px = centerX + c;
                if (isFunction[py, px]) continue;
                isFunction[py, px] = true;
                modules[py, px] = (Math.Abs(r) == 2 || Math.Abs(c) == 2 || (r == 0 && c == 0));
            }
        }
    }

    private static void WriteFormatBits(bool[,] modules, int formatBits, int size)
    {
        int[] order1 = [0, 1, 2, 3, 4, 5, 7, 8];
        int[] order2 = [size - 1, size - 2, size - 3, size - 4, size - 5, size - 6, size - 7];

        for (int i = 0; i < 15; i++)
        {
            bool bit = ((formatBits >> i) & 1) == 1;
            if (i < 6) modules[i, 8] = bit;
            else if (i == 6) modules[7, 8] = bit;
            else if (i == 7) modules[8, 8] = bit;
            else if (i == 8) modules[8, 7] = bit;
            else modules[8, 14 - i] = bit;

            if (i < 7) modules[8, size - 1 - i] = bit;
            else modules[size - 15 + i, 8] = bit;
        }
    }

    private static byte[] EncodeCodewords(byte[] data, int version)
    {
        int totalCodewords = version switch
        {
            1 => 26,
            2 => 44,
            3 => 70,
            4 => 100,
            5 => 134,
            6 => 172,
            _ => 196
        };
        int dataCodewords = version switch
        {
            1 => 16,
            2 => 28,
            3 => 44,
            4 => 64,
            5 => 86,
            6 => 108,
            _ => 124
        };
        int ecCodewords = totalCodewords - dataCodewords;

        using var ms = new MemoryStream();
        // Mode indicator 0100 (Byte mode)
        int bitBuffer = 0b0100;
        int bitCount = 4;

        // Character count indicator (8 bits for Version 1..9 in byte mode)
        bitBuffer = (bitBuffer << 8) | (data.Length & 0xFF);
        bitCount += 8;

        while (bitCount >= 8)
        {
            bitCount -= 8;
            ms.WriteByte((byte)((bitBuffer >> bitCount) & 0xFF));
        }

        // Write data bytes
        foreach (byte b in data)
        {
            bitBuffer = (bitBuffer << 8) | b;
            bitCount += 8;
            while (bitCount >= 8)
            {
                bitCount -= 8;
                ms.WriteByte((byte)((bitBuffer >> bitCount) & 0xFF));
            }
        }

        // Terminator (up to 4 zero bits)
        bitBuffer <<= Math.Min(4, 8 - bitCount);
        bitCount += Math.Min(4, 8 - bitCount);

        // Pad to byte boundary
        if (bitCount > 0)
        {
            bitBuffer <<= (8 - bitCount);
            ms.WriteByte((byte)(bitBuffer & 0xFF));
        }

        // Pad bytes 0xEC, 0x11
        byte[] padBytes = [0xEC, 0x11];
        int padIdx = 0;
        while (ms.Length < dataCodewords)
        {
            ms.WriteByte(padBytes[padIdx % 2]);
            padIdx++;
        }

        byte[] dataBytes = ms.ToArray();
        byte[] ecBytes = ComputeReedSolomon(dataBytes, ecCodewords);

        byte[] result = new byte[totalCodewords];
        Array.Copy(dataBytes, 0, result, 0, dataBytes.Length);
        Array.Copy(ecBytes, 0, result, dataBytes.Length, ecBytes.Length);
        return result;
    }

    private static byte[] ComputeReedSolomon(byte[] data, int ecCount)
    {
        // Standard GF(256) with primitive polynomial 0x11D (285)
        byte[] exp = new byte[512];
        byte[] log = new byte[256];
        int x = 1;
        for (int i = 0; i < 255; i++)
        {
            exp[i] = (byte)x;
            exp[i + 255] = (byte)x;
            log[x] = (byte)i;
            x = (x << 1) ^ (x >= 128 ? 0x11D : 0);
        }

        // Generator polynomial for ecCount
        byte[] gen = [1];
        for (int i = 0; i < ecCount; i++)
        {
            byte[] next = new byte[gen.Length + 1];
            for (int j = 0; j < gen.Length; j++)
            {
                next[j] ^= exp[(log[gen[j]] + i) % 255];
                next[j + 1] ^= gen[j];
            }
            gen = next;
        }

        byte[] res = new byte[ecCount];
        foreach (byte b in data)
        {
            byte factor = (byte)(b ^ res[0]);
            Array.Copy(res, 1, res, 0, ecCount - 1);
            res[ecCount - 1] = 0;
            if (factor != 0)
            {
                int logFactor = log[factor];
                for (int j = 0; j < ecCount; j++)
                {
                    res[j] ^= exp[(log[gen[j]] + logFactor) % 255];
                }
            }
        }
        return res;
    }

    public static string ToSvg(bool[,] modules, int pixelSize = 4, int quietZone = 2)
    {
        int size = modules.GetLength(0);
        int totalSize = (size + quietZone * 2) * pixelSize;

        var sb = new StringBuilder();
        sb.Append($"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {totalSize} {totalSize}\" width=\"100%\" height=\"100%\" shape-rendering=\"crispEdges\">");
        sb.Append($"<rect width=\"{totalSize}\" height=\"{totalSize}\" fill=\"#ffffff\"/>");

        for (int r = 0; r < size; r++)
        {
            for (int c = 0; c < size; c++)
            {
                if (modules[r, c])
                {
                    int x = (c + quietZone) * pixelSize;
                    int y = (r + quietZone) * pixelSize;
                    sb.Append($"<rect x=\"{x}\" y=\"{y}\" width=\"{pixelSize}\" height=\"{pixelSize}\" fill=\"#000000\"/>");
                }
            }
        }
        sb.Append("</svg>");
        return sb.ToString();
    }

    public static byte[] ToBmp(bool[,] modules, int pixelSize = 4, int quietZone = 2)
    {
        int size = modules.GetLength(0);
        int dim = (size + quietZone * 2) * pixelSize;
        int rowStride = (dim * 3 + 3) & ~3; // 4-byte aligned
        int imageSize = rowStride * dim;
        int fileSize = 54 + imageSize;

        byte[] bmp = new byte[fileSize];

        // BMP Header
        bmp[0] = (byte)'B'; bmp[1] = (byte)'M';
        BitConverter.GetBytes(fileSize).CopyTo(bmp, 2);
        BitConverter.GetBytes(54).CopyTo(bmp, 10); // Offset

        // DIB Header (BITMAPINFOHEADER 40 bytes)
        BitConverter.GetBytes(40).CopyTo(bmp, 14);
        BitConverter.GetBytes(dim).CopyTo(bmp, 18);
        BitConverter.GetBytes(dim).CopyTo(bmp, 22);
        BitConverter.GetBytes((short)1).CopyTo(bmp, 26); // Planes
        BitConverter.GetBytes((short)24).CopyTo(bmp, 28); // 24-bit RGB
        BitConverter.GetBytes(0).CopyTo(bmp, 30); // Compression
        BitConverter.GetBytes(imageSize).CopyTo(bmp, 34);
        BitConverter.GetBytes(2835).CopyTo(bmp, 38); // 72 DPI
        BitConverter.GetBytes(2835).CopyTo(bmp, 42);

        // Pixel data (bottom-to-top)
        for (int py = 0; py < dim; py++)
        {
            int r = size - 1 - ((py / pixelSize) - quietZone);
            int rowStart = 54 + py * rowStride;

            for (int px = 0; px < dim; px++)
            {
                int c = (px / pixelSize) - quietZone;
                bool isBlack = r >= 0 && r < size && c >= 0 && c < size && modules[r, c];
                byte color = isBlack ? (byte)0 : (byte)255;

                int pixelOffset = rowStart + px * 3;
                bmp[pixelOffset] = color;     // Blue
                bmp[pixelOffset + 1] = color; // Green
                bmp[pixelOffset + 2] = color; // Red
            }
        }

        return bmp;
    }
}
