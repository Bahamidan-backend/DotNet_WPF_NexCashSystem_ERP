using POSSystem.WPF.Services.Logging;
using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using System.Collections.Generic;
using System.IO;

namespace POSSystem.WPF.Helpers
{
    public class RawPrinterHelper
    {
        [DllImport("winspool.Drv", EntryPoint = "OpenPrinterA")]
        public static extern bool OpenPrinter(string szPrinter, out IntPtr hPrinter, IntPtr pd);

        [DllImport("winspool.Drv", EntryPoint = "ClosePrinter")]
        public static extern bool ClosePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "StartDocPrinterA")]
        public static extern bool StartDocPrinter(IntPtr hPrinter, int level, IntPtr di);

        [DllImport("winspool.Drv", EntryPoint = "EndDocPrinter")]
        public static extern bool EndDocPrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "StartPagePrinter")]
        public static extern bool StartPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "EndPagePrinter")]
        public static extern bool EndPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "WritePrinter")]
        public static extern bool WritePrinter(IntPtr hPrinter, byte[] buffer, int buf, out int pcWritten);

        [DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool SetPrinter(IntPtr hPrinter, int Level, IntPtr pPrinter, int Command);

        public const int PRINTER_CONTROL_PURGE = 3;

        [DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool GetDefaultPrinter(StringBuilder pszBuffer, ref int pcchBuffer);

        public static string GetDefaultPrinterName()
        {
            try
            {
                int pcchBuffer = 0;
                GetDefaultPrinter(null!, ref pcchBuffer);
                if (pcchBuffer > 0)
                {
                    var sb = new StringBuilder(pcchBuffer);
                    if (GetDefaultPrinter(sb, ref pcchBuffer))
                    {
                        return sb.ToString();
                    }
                }
            }
            catch { }
            return string.Empty;
        }

        public static bool IsPrinterInstalled(string? printerName)
        {
            if (string.IsNullOrWhiteSpace(printerName)) return false;
            try
            {
                IntPtr hPrinter;
                if (OpenPrinter(printerName.Trim(), out hPrinter, IntPtr.Zero))
                {
                    ClosePrinter(hPrinter);
                    return true;
                }
            }
            catch { }
            return false;
        }

        public static List<string> GetInstalledPrinters()
        {
            var list = new List<string>();
            try
            {
                var def = GetDefaultPrinterName();
                if (!string.IsNullOrWhiteSpace(def))
                    list.Add(def);

                using var context = new POSSystem.WPF.Data.POSDbContext();
                var kitchenPrinters = Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AsNoTracking(context.Kitchens)
                    .Where(k => !string.IsNullOrWhiteSpace(k.PrinterName))
                    .Select(k => k.PrinterName!)
                    .Distinct()
                    .ToList();
                foreach (var kp in kitchenPrinters)
                {
                    if (!list.Contains(kp, StringComparer.OrdinalIgnoreCase))
                        list.Add(kp);
                }

                using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Print\Printers");
                if (key != null)
                {
                    foreach (var sub in key.GetSubKeyNames())
                    {
                        if (!string.IsNullOrWhiteSpace(sub) && !list.Contains(sub, StringComparer.OrdinalIgnoreCase))
                            list.Add(sub);
                    }
                }
            }
            catch { }
            return list;
        }


        public static bool PurgePrinterQueue(string printerName)
        {
            if (string.IsNullOrWhiteSpace(printerName)) return false;
            try
            {
                IntPtr hPrinter;
                if (OpenPrinter(printerName, out hPrinter, IntPtr.Zero))
                {
                    bool success = SetPrinter(hPrinter, 0, IntPtr.Zero, PRINTER_CONTROL_PURGE);
                    ClosePrinter(hPrinter);
                    return success;
                }
            }
            catch (Exception ex)
            {
                AppLogger.LogWarning($"PurgePrinterQueue Error: {ex.Message}");
            }
            return false;
        }

        public static bool Print(string printerName, string text)
        {
            // For Arabic, we now prefer PrintAsBitmap to guarantee correct rendering.
            // If the text contains Arabic characters, we switch to bitmap mode.
            if (ContainsArabic(text))
            {
                return PrintAsBitmap(printerName, text);
            }
            return PrintRaw(printerName, text);
        }

        private static bool ContainsArabic(string text)
        {
            foreach (char c in text)
            {
                if (c >= 0x0600 && c <= 0x06FF) return true;
                if (c >= 0xFB50 && c <= 0xFDFF) return true;
                if (c >= 0xFE70 && c <= 0xFEFF) return true;
            }
            return false;
        }

        public static bool PrintRaw(string printerName, string text)
        {
            try 
            {
                IntPtr printer;
                if (!OpenPrinter(printerName, out printer, IntPtr.Zero))
                {
                    return false;
                }

                DOCINFOA di = new DOCINFOA();
                di.pDocName = "Receipt";
                di.pDataType = "RAW";

                if (StartDocPrinter(printer, 1, di.GetIntPtr()))
                {
                    if (StartPagePrinter(printer))
                    {
                        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                        try
                        {
                            byte[] initCommand = new byte[] { 0x1B, 0x40, 0x1C, 0x2E, 0x1B, 0x74, 0x16 };
                            Encoding arabicEncoding = Encoding.GetEncoding(1256);
                            byte[] textBytes = arabicEncoding.GetBytes(text);
                            byte[] bytesToPrint = new byte[initCommand.Length + textBytes.Length];
                            Buffer.BlockCopy(initCommand, 0, bytesToPrint, 0, initCommand.Length);
                            Buffer.BlockCopy(textBytes, 0, bytesToPrint, initCommand.Length, textBytes.Length);

                            int written;
                            WritePrinter(printer, bytesToPrint, bytesToPrint.Length, out written);
                        }
                        catch (Exception)
                        {
                            byte[] bytes = Encoding.UTF8.GetBytes(text);
                            int written;
                            WritePrinter(printer, bytes, bytes.Length, out written);
                        }

                        EndPagePrinter(printer);
                    }
                    EndDocPrinter(printer);
                }
                ClosePrinter(printer);
                return true;
            }
            catch(Exception)
            {
                return false;
            }
        }

        public static bool PrintAsBitmap(string printerName, string text)
        {
            try
            {
                // Separate text from potential ESC/POS commands
                string printableText = text;
                string commands = "";
                int firstCmd = text.IndexOfAny(new char[] { '\x1B', '\x1D', '\x1C' });
                if (firstCmd >= 0)
                {
                    printableText = text.Substring(0, firstCmd);
                    commands = text.Substring(firstCmd);
                }

                TextBlock textBlock = new TextBlock
                {
                    Text = printableText,
                    FontFamily = new FontFamily("Arial"),
                    FontSize = 24,
                    TextWrapping = TextWrapping.Wrap,
                    Width = 580,
                    Background = Brushes.White,
                    Foreground = Brushes.Black,
                    Padding = new Thickness(10, 0, 10, 0),
                    FlowDirection = FlowDirection.RightToLeft
                };

                return PrintVisual(printerName, textBlock, commands);
            }
            catch (Exception ex)
            {
                AppLogger.LogError("Bitmap Print Error: " + ex.Message, ex);
                return PrintRaw(printerName, text);
            }
        }

        public static bool PrintVisual(string printerName, Visual visual, string extraCommands = "")
        {
            try
            {
                var dispatcher = Application.Current?.Dispatcher;
                if (dispatcher != null && !dispatcher.CheckAccess())
                {
                    return dispatcher.Invoke(() => PrintVisual(printerName, visual, extraCommands));
                }
                // 1. Determine size and render
                FrameworkElement element = visual as FrameworkElement;
                if (element != null)
                {
                    if (element.ActualWidth == 0 || element.ActualHeight == 0)
                    {
                        element.Measure(new Size(580, double.PositiveInfinity));
                        element.Arrange(new Rect(0, 0, element.DesiredSize.Width, element.DesiredSize.Height));
                    }
                }

                double width = 580;
                double height = 500;
                if (element != null)
                {
                    width = element.ActualWidth > 0 ? element.ActualWidth : (element.Width > 0 ? element.Width : 580);
                    height = element.ActualHeight;
                }

                RenderTargetBitmap rtb = new RenderTargetBitmap((int)width, (int)height, 96, 96, PixelFormats.Pbgra32);
                rtb.Render(visual);

                // 2. Convert to Raster
                FormatConvertedBitmap grayBitmap = new FormatConvertedBitmap(rtb, PixelFormats.Gray8, null, 0);
                byte[] rasterBytes = GetEscPosRasterBytes(grayBitmap);

                // 3. Send to Printer
                IntPtr hPrinter;
                if (!OpenPrinter(printerName, out hPrinter, IntPtr.Zero)) return false;

                DOCINFOA di = new DOCINFOA { pDocName = "VisualReceipt", pDataType = "RAW" };
                if (StartDocPrinter(hPrinter, 1, di.GetIntPtr()))
                {
                    if (StartPagePrinter(hPrinter))
                    {
                        int written;
                        WritePrinter(hPrinter, new byte[] { 0x1B, 0x40 }, 2, out written);
                        WritePrinter(hPrinter, rasterBytes, rasterBytes.Length, out written);
                        WritePrinter(hPrinter, new byte[] { 0x0A, 0x0A }, 2, out written);

                        if (!string.IsNullOrEmpty(extraCommands))
                        {
                            byte[] cmdBytes = Encoding.ASCII.GetBytes(extraCommands);
                            WritePrinter(hPrinter, cmdBytes, cmdBytes.Length, out written);
                        }

                        EndPagePrinter(hPrinter);
                    }
                    EndDocPrinter(hPrinter);
                }
                ClosePrinter(hPrinter);
                return true;
            }
            catch (Exception ex)
            {
                AppLogger.LogError("Visual Print Error: " + ex.Message, ex);
                return false;
            }
        }

        private static byte[] GetEscPosRasterBytes(BitmapSource bitmap)
        {
            int width = bitmap.PixelWidth;
            int height = bitmap.PixelHeight;

            // Width must be a multiple of 8
            int widthInBytes = (width + 7) / 8;
            byte[] pixels = new byte[width * height];
            bitmap.CopyPixels(pixels, width, 0);

            MemoryStream ms = new MemoryStream();
            BinaryWriter bw = new BinaryWriter(ms);

            // GS v 0 m xL xH yL yH
            // m=0 (normal), xL/xH = width in bytes, yL/yH = height in dots
            bw.Write(new byte[] { 0x1D, 0x76, 0x30, 0x00 });
            bw.Write((byte)(widthInBytes % 256));
            bw.Write((byte)(widthInBytes / 256));
            bw.Write((byte)(height % 256));
            bw.Write((byte)(height / 256));

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < widthInBytes; x++)
                {
                    byte b = 0;
                    for (int bit = 0; bit < 8; bit++)
                    {
                        int px = x * 8 + bit;
                        if (px < width)
                        {
                            // In Gray8, 0 is black, 255 is white. 
                            // Receipt printers use 1 for black, 0 for white in raster mode.
                            if (pixels[y * width + px] < 128)
                            {
                                b |= (byte)(1 << (7 - bit));
                            }
                        }
                    }
                    bw.Write(b);
                }
            }

            return ms.ToArray();
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public class DOCINFOA
        {
            [MarshalAs(UnmanagedType.LPStr)]
            public string pDocName;
            [MarshalAs(UnmanagedType.LPStr)]
            public string pOutputFile;
            [MarshalAs(UnmanagedType.LPStr)]
            public string pDataType;

             public IntPtr GetIntPtr()
            {
                IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(this));
                Marshal.StructureToPtr(this, ptr, false);
                return ptr;
            }
        }
    }
}
