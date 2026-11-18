using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace POSSystem.WPF.Helpers
{
    /// <summary>
    /// ZATCA (هيئة الزكاة والضريبة والجمارك) QR Code Generator for Simplified Tax Invoices
    /// Implements TLV (Tag-Length-Value) Base64 encoding + Pure C# QR Code matrix rendering.
    /// </summary>
    public static class ZatcaQrCodeHelper
    {
        /// <summary>
        /// Generates the standard ZATCA Phase 1 & 2 TLV-encoded Base64 string.
        /// </summary>
        public static string GenerateZatcaBase64(string sellerName, string vatNumber, DateTime timestamp, decimal totalWithVat, decimal vatTotal)
        {
            var tlv1 = GetTlv(1, sellerName);
            var tlv2 = GetTlv(2, vatNumber);
            var tlv3 = GetTlv(3, timestamp.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture));
            var tlv4 = GetTlv(4, totalWithVat.ToString("F2", CultureInfo.InvariantCulture));
            var tlv5 = GetTlv(5, vatTotal.ToString("F2", CultureInfo.InvariantCulture));

            var bytes = new List<byte>();
            bytes.AddRange(tlv1);
            bytes.AddRange(tlv2);
            bytes.AddRange(tlv3);
            bytes.AddRange(tlv4);
            bytes.AddRange(tlv5);

            return Convert.ToBase64String(bytes.ToArray());
        }

        private static byte[] GetTlv(int tag, string value)
        {
            byte[] valueBytes = Encoding.UTF8.GetBytes(value ?? string.Empty);
            byte[] result = new byte[2 + valueBytes.Length];
            result[0] = (byte)tag;
            result[1] = (byte)valueBytes.Length;
            Array.Copy(valueBytes, 0, result, 2, valueBytes.Length);
            return result;
        }

        /// <summary>
        /// Renders a QR code as a WPF BitmapSource for thermal printing or UI display.
        /// Uses a pure, self-contained QR encoder without external dependencies.
        /// </summary>
        public static BitmapSource GenerateQrBitmap(string content, int moduleSize = 5)
        {
            bool[,] matrix = SimpleQrEncoder.Encode(content);
            int width = matrix.GetLength(0);
            int height = matrix.GetLength(1);

            int pixelWidth = (width + 4) * moduleSize; // 2 modules quiet zone on each side
            int pixelHeight = (height + 4) * moduleSize;

            WriteableBitmap bitmap = new WriteableBitmap(pixelWidth, pixelHeight, 96, 96, PixelFormats.Bgr32, null);
            int[] pixels = new int[pixelWidth * pixelHeight];

            int black = 0x000000;
            int white = unchecked((int)0xFFFFFFFF);

            // Fill with white
            for (int i = 0; i < pixels.Length; i++) pixels[i] = white;

            // Draw modules
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (matrix[x, y])
                    {
                        int startX = (x + 2) * moduleSize;
                        int startY = (y + 2) * moduleSize;

                        for (int py = 0; py < moduleSize; py++)
                        {
                            for (int px = 0; px < moduleSize; px++)
                            {
                                int idx = (startY + py) * pixelWidth + (startX + px);
                                if (idx >= 0 && idx < pixels.Length)
                                    pixels[idx] = black;
                            }
                        }
                    }
                }
            }

            bitmap.WritePixels(new Int32Rect(0, 0, pixelWidth, pixelHeight), pixels, pixelWidth * 4, 0);
            bitmap.Freeze();
            return bitmap;
        }

        #region Minimal Self-Contained QR Code Encoder
        /// <summary>
        /// A lightweight, robust ISO/IEC 18004 QR Code Matrix Encoder for byte payloads.
        /// </summary>
        private static class SimpleQrEncoder
        {
            public static bool[,] Encode(string text)
            {
                byte[] data = Encoding.UTF8.GetBytes(text);
                int version = ChooseVersion(data.Length);
                int size = 17 + 4 * version;
                bool[,] matrix = new bool[size, size];
                bool[,] isFunction = new bool[size, size];

                // 1. Finder patterns
                DrawFinderPattern(matrix, isFunction, 0, 0);
                DrawFinderPattern(matrix, isFunction, size - 7, 0);
                DrawFinderPattern(matrix, isFunction, 0, size - 7);

                // 2. Alignment patterns for version >= 2
                if (version >= 2)
                {
                    int[] alignPos = GetAlignmentPatternPositions(version);
                    foreach (int r in alignPos)
                    {
                        foreach (int c in alignPos)
                        {
                            if (isFunction[r, c]) continue;
                            DrawAlignmentPattern(matrix, isFunction, r, c);
                        }
                    }
                }

                // 3. Timing patterns
                for (int i = 0; i < size; i++)
                {
                    SetFunctionModule(matrix, isFunction, 6, i, i % 2 == 0);
                    SetFunctionModule(matrix, isFunction, i, 6, i % 2 == 0);
                }

                // 4. Reserve format and version areas
                ReserveFormatAreas(isFunction, size, version);

                // 5. Data encoding with Reed-Solomon Error Correction (Level L)
                byte[] codewords = GenerateCodewords(data, version);

                // 6. Place data bits using zig-zag scan
                PlaceDataBits(matrix, isFunction, codewords, size);

                // 7. Apply standard mask pattern (Mask 0: (row + col) % 2 == 0)
                ApplyMask(matrix, isFunction, size);

                // 8. Write format bits
                WriteFormatBits(matrix, size);

                return matrix;
            }

            private static int ChooseVersion(int dataLen)
            {
                // Level L data capacities for byte mode:
                int[] capacities = { 0, 17, 32, 53, 78, 106, 134, 154, 192, 230, 271, 321, 367, 425, 458, 520, 586, 644, 718, 792, 858 };
                for (int v = 1; v < capacities.Length; v++)
                {
                    if (dataLen <= capacities[v]) return v;
                }
                return 10; // Fallback
            }

            private static void DrawFinderPattern(bool[,] m, bool[,] f, int r, int c)
            {
                for (int y = -1; y <= 7; y++)
                {
                    for (int x = -1; x <= 7; x++)
                    {
                        int row = r + y;
                        int col = c + x;
                        if (row >= 0 && row < m.GetLength(0) && col >= 0 && col < m.GetLength(1))
                        {
                            bool val = (x >= 0 && x <= 6 && (y == 0 || y == 6)) ||
                                       (y >= 0 && y <= 6 && (x == 0 || x == 6)) ||
                                       (x >= 2 && x <= 4 && y >= 2 && y <= 4);
                            SetFunctionModule(m, f, row, col, val);
                        }
                    }
                }
            }

            private static void DrawAlignmentPattern(bool[,] m, bool[,] f, int centerR, int centerC)
            {
                for (int y = -2; y <= 2; y++)
                {
                    for (int x = -2; x <= 2; x++)
                    {
                        bool val = Math.Max(Math.Abs(x), Math.Abs(y)) != 1;
                        SetFunctionModule(m, f, centerR + y, centerC + x, val);
                    }
                }
            }

            private static int[] GetAlignmentPatternPositions(int version)
            {
                if (version == 1) return Array.Empty<int>();
                int size = 17 + 4 * version;
                int intervals = version / 7 + 1;
                int step = (size - 7 - 6 + intervals) / intervals;
                step = (step + 1) / 2 * 2;
                List<int> pos = new() { 6 };
                for (int p = size - 7; p > 6; p -= step)
                    pos.Insert(1, p);
                return pos.ToArray();
            }

            private static void SetFunctionModule(bool[,] m, bool[,] f, int r, int c, bool val)
            {
                m[r, c] = val;
                f[r, c] = true;
            }

            private static void ReserveFormatAreas(bool[,] f, int size, int version)
            {
                for (int i = 0; i < 9; i++)
                {
                    if (i < size) { f[8, i] = true; f[i, 8] = true; }
                }
                for (int i = 0; i < 8; i++)
                {
                    f[8, size - 1 - i] = true;
                    f[size - 1 - i, 8] = true;
                }
                f[size - 8, 8] = true; // Dark module
            }

            private static byte[] GenerateCodewords(byte[] data, int version)
            {
                int totalCodewords = GetTotalCodewords(version);
                int ecCodewords = GetEcCodewords(version);
                int dataCodewords = totalCodewords - ecCodewords;

                // 1. Bit buffer with 0100 (Byte mode) + Length + Payload + Terminator
                List<bool> bits = new();
                bits.AddRange(new[] { false, true, false, false }); // Mode 4 (Byte)
                int countBits = version < 10 ? 8 : 16;
                for (int i = countBits - 1; i >= 0; i--)
                    bits.Add(((data.Length >> i) & 1) != 0);

                foreach (byte b in data)
                {
                    for (int i = 7; i >= 0; i--)
                        bits.Add(((b >> i) & 1) != 0);
                }

                // Terminator
                while (bits.Count < dataCodewords * 8 && bits.Count % 8 != 0) bits.Add(false);
                while (bits.Count < dataCodewords * 8 && bits.Count < dataCodewords * 8 - 4)
                {
                    bits.AddRange(new[] { false, false, false, false });
                }

                List<byte> dc = new();
                for (int i = 0; i < bits.Count; i += 8)
                {
                    byte b = 0;
                    for (int j = 0; j < 8 && i + j < bits.Count; j++)
                    {
                        if (bits[i + j]) b |= (byte)(1 << (7 - j));
                    }
                    dc.Add(b);
                }

                // Pad bytes (0xEC, 0x11)
                byte[] pad = { 0xEC, 0x11 };
                int pIdx = 0;
                while (dc.Count < dataCodewords)
                {
                    dc.Add(pad[pIdx % 2]);
                    pIdx++;
                }

                // 2. Compute Reed-Solomon Error Correction
                byte[] ec = ComputeReedSolomon(dc.ToArray(), ecCodewords);

                List<byte> result = new();
                result.AddRange(dc);
                result.AddRange(ec);
                return result.ToArray();
            }

            private static int GetTotalCodewords(int version) => version switch
            {
                1 => 26, 2 => 44, 3 => 70, 4 => 100, 5 => 134,
                6 => 172, 7 => 196, 8 => 242, 9 => 292, 10 => 346,
                _ => 346
            };

            private static int GetEcCodewords(int version) => version switch
            {
                1 => 7, 2 => 10, 3 => 15, 4 => 20, 5 => 26,
                6 => 36, 7 => 40, 8 => 48, 9 => 60, 10 => 72,
                _ => 72
            };

            private static byte[] ComputeReedSolomon(byte[] data, int ecCount)
            {
                byte[] generator = GetGeneratorPolynomial(ecCount);
                byte[] info = new byte[data.Length + ecCount];
                Array.Copy(data, info, data.Length);

                for (int i = 0; i < data.Length; i++)
                {
                    byte lead = info[i];
                    if (lead != 0)
                    {
                        for (int j = 0; j < generator.Length; j++)
                        {
                            info[i + j] ^= GfMul(generator[j], lead);
                        }
                    }
                }

                byte[] ec = new byte[ecCount];
                Array.Copy(info, data.Length, ec, 0, ecCount);
                return ec;
            }

            private static byte[] GetGeneratorPolynomial(int degree)
            {
                byte[] poly = { 1 };
                for (int i = 0; i < degree; i++)
                {
                    byte root = GfPow(2, i);
                    byte[] next = new byte[poly.Length + 1];
                    for (int j = 0; j < poly.Length; j++)
                    {
                        next[j] ^= GfMul(poly[j], root);
                        next[j + 1] ^= poly[j];
                    }
                    poly = next;
                }
                return poly;
            }

            private static byte GfMul(byte a, byte b)
            {
                if (a == 0 || b == 0) return 0;
                return ExpTable[(LogTable[a] + LogTable[b]) % 255];
            }

            private static byte GfPow(byte a, int b)
            {
                if (b == 0) return 1;
                if (a == 0) return 0;
                return ExpTable[(LogTable[a] * b) % 255];
            }

            private static readonly byte[] ExpTable = new byte[256];
            private static readonly byte[] LogTable = new byte[256];

            static SimpleQrEncoder()
            {
                int val = 1;
                for (int i = 0; i < 255; i++)
                {
                    ExpTable[i] = (byte)val;
                    LogTable[val] = (byte)i;
                    val <<= 1;
                    if ((val & 0x100) != 0) val ^= 0x11D;
                }
                ExpTable[255] = ExpTable[0];
            }

            private static void PlaceDataBits(bool[,] matrix, bool[,] isFunc, byte[] codewords, int size)
            {
                int bitIndex = 0;
                int totalBits = codewords.Length * 8;
                int x = size - 1;

                while (x > 0)
                {
                    if (x == 6) x--; // Skip timing col
                    for (int vert = 0; vert < size; vert++)
                    {
                        for (int h = 0; h < 2; h++)
                        {
                            int col = x - h;
                            int row = ((x + 1) / 2 % 2 == 1) ? (size - 1 - vert) : vert;

                            if (!isFunc[row, col])
                            {
                                bool bit = false;
                                if (bitIndex < totalBits)
                                {
                                    int byteIdx = bitIndex / 8;
                                    int subBit = 7 - (bitIndex % 8);
                                    bit = ((codewords[byteIdx] >> subBit) & 1) != 0;
                                    bitIndex++;
                                }
                                matrix[row, col] = bit;
                            }
                        }
                    }
                    x -= 2;
                }
            }

            private static void ApplyMask(bool[,] matrix, bool[,] isFunc, int size)
            {
                for (int r = 0; r < size; r++)
                {
                    for (int c = 0; c < size; c++)
                    {
                        if (!isFunc[r, c])
                        {
                            // Mask pattern 0: (row + column) % 2 == 0
                            if ((r + c) % 2 == 0)
                                matrix[r, c] = !matrix[r, c];
                        }
                    }
                }
            }

            private static void WriteFormatBits(bool[,] matrix, int size)
            {
                // Level L, Mask 0 format bits with BCH code: 111011111000100 (binary)
                int formatBits = 0b111011111000100;
                for (int i = 0; i < 15; i++)
                {
                    bool bit = ((formatBits >> (14 - i)) & 1) != 0;
                    // Top-left
                    if (i < 6) matrix[8, i] = bit;
                    else if (i < 8) matrix[8, i + 1] = bit;
                    else if (i == 8) matrix[7, 8] = bit;
                    else matrix[14 - i, 8] = bit;

                    // Bottom-left / Top-right
                    if (i < 8) matrix[size - 1 - i, 8] = bit;
                    else matrix[8, size - 15 + i] = bit;
                }
                // Dark module
                matrix[size - 8, 8] = true;
            }
        }
        #endregion
    }
}

