using ClosedXML.Excel;
using Microsoft.Win32;
using POSSystem.WPF.Services.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;

namespace POSSystem.WPF.Services
{
    public class PrintService
    {
        private static string SanitizeFileName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "تقرير";
            var invalid = Path.GetInvalidFileNameChars();
            var clean = new string(name.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
            return clean.Replace(' ', '_');
        }

        private static bool TryParseNumericCell(string? raw, out decimal numericVal)
        {
            numericVal = 0m;
            if (string.IsNullOrWhiteSpace(raw)) return false;

            string s = raw.Trim();
            // Do not convert phone numbers, dates, or reference codes with leading zeros or hyphens
            if (s.Contains('/') || s.Contains(':') || s.StartsWith("0") && s.Length > 4)
                return false;

            s = s.Replace("ريال يمني", "")
                 .Replace("ريال", "")
                 .Replace("ر.ي", "")
                 .Replace("ر.س", "")
                 .Replace("٬", "")
                 .Replace(",", "")
                 .Trim();

            if (s.Length == 0) return false;
            return decimal.TryParse(s, NumberStyles.Number | NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out numericVal);
        }

        private static void StyleTitleAndSummaryBanner(IXLWorksheet ws, string title, string subtitle, int colCount, ref int currentRow)
        {
            int cols = Math.Max(colCount, 4);

            // Row 1: Main Title Banner (#00358C)
            ws.Cell(currentRow, 1).Value = title;
            ws.Range(currentRow, 1, currentRow, cols).Merge();
            var titleRange = ws.Range(currentRow, 1, currentRow, cols);
            titleRange.Style.Font.Bold = true;
            titleRange.Style.Font.FontSize = 15;
            titleRange.Style.Font.FontColor = XLColor.White;
            titleRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#00358C");
            titleRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            titleRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            ws.Row(currentRow).Height = 32;
            currentRow++;

            // Row 2: Subtitle + Export Timestamp Banner (#E0F2FE)
            string subText = string.IsNullOrWhiteSpace(subtitle)
                ? $"تاريخ التصدير: {DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)}"
                : $"{subtitle}   |   تاريخ التصدير: {DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)}";

            ws.Cell(currentRow, 1).Value = subText;
            ws.Range(currentRow, 1, currentRow, cols).Merge();
            var subRange = ws.Range(currentRow, 1, currentRow, cols);
            subRange.Style.Font.Bold = true;
            subRange.Style.Font.FontSize = 11;
            subRange.Style.Font.FontColor = XLColor.FromHtml("#1E293B");
            subRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#E0F2FE");
            subRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            subRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            ws.Row(currentRow).Height = 24;
            currentRow += 2;
        }

        private static void WriteSummaryFooter(IXLWorksheet ws, string? summary, int colCount, ref int currentRow)
        {
            if (string.IsNullOrWhiteSpace(summary)) return;

            int cols = Math.Max(colCount, 4);
            currentRow++;
            var lines = summary.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                if (line.Trim() == "---") continue;
                ws.Cell(currentRow, 1).Value = line.Trim();
                ws.Range(currentRow, 1, currentRow, cols).Merge();
                var range = ws.Range(currentRow, 1, currentRow, cols);
                range.Style.Font.Bold = true;
                range.Style.Font.FontSize = 11;
                range.Style.Font.FontColor = XLColor.FromHtml("#0F172A");
                range.Style.Fill.BackgroundColor = XLColor.FromHtml("#F1F5F9");
                range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                for (int c = 1; c <= cols; c++)
                {
                    ws.Cell(currentRow, c).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    ws.Cell(currentRow, c).Style.Border.OutsideBorderColor = XLColor.FromHtml("#CBD5E1");
                }
                ws.Row(currentRow).Height = 23;
                currentRow++;
            }
        }

        private static void AutoFitArabicColumns(IXLWorksheet ws, int colCount)
        {
            // Measure up to first 250 rows so large reports (10K+ rows) export in <50ms without UI freeze
            ws.Columns().AdjustToContents(1, 250);
            for (int c = 1; c <= colCount; c++)
            {
                if (ws.Column(c).Width < 16)
                    ws.Column(c).Width = 16;
                if (ws.Column(c).Width > 50)
                    ws.Column(c).Width = 50;
            }
        }

        public static void PrintReport(
            string title,
            string subtitle,
            System.Collections.IEnumerable data,
            string[] columnHeaders,
            Func<object, string[]> getRowData,
            string summary = "",
            bool preferPdf = false)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(title)) title = "تقرير";
                if (columnHeaders == null || columnHeaders.Length == 0)
                {
                    MessageBox.Show("لا توجد أعمدة محددة للتصدير", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (data == null)
                {
                    MessageBox.Show("لا توجد بيانات للتصدير", "معلومة", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var dlg = new SaveFileDialog
                {
                    Filter = "ملف إكسل منسق (*.xlsx)|*.xlsx",
                    DefaultExt = ".xlsx",
                    FileName = $"{SanitizeFileName(title)}_{DateTime.Now.ToString("yyyyMMdd_HHmm", CultureInfo.InvariantCulture)}.xlsx"
                };

                if (dlg.ShowDialog() != true) return;

                using var wb = new XLWorkbook();
                var ws = wb.Worksheets.Add("التقرير");
                ws.RightToLeft = true;

                int colCount = columnHeaders.Length;
                int rowIdx = 1;
                StyleTitleAndSummaryBanner(ws, title, subtitle, colCount, ref rowIdx);

                // Table Headers
                for (int c = 0; c < colCount; c++)
                {
                    var cell = ws.Cell(rowIdx, c + 1);
                    cell.Value = columnHeaders[c];
                    cell.Style.Font.Bold = true;
                    cell.Style.Font.FontSize = 11;
                    cell.Style.Font.FontColor = XLColor.White;
                    cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E3A8A");
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    cell.Style.Border.OutsideBorderColor = XLColor.FromHtml("#CBD5E1");
                }
                ws.Row(rowIdx).Height = 26;
                rowIdx++;

                int dataRowsCount = 0;
                foreach (var item in data)
                {
                    if (item == null) continue;
                    var rowValues = getRowData(item);
                    if (rowValues == null) continue;

                    for (int c = 0; c < colCount && c < rowValues.Length; c++)
                    {
                        var cell = ws.Cell(rowIdx, c + 1);
                        string valStr = rowValues[c] ?? "";
                        if (TryParseNumericCell(valStr, out decimal numVal))
                        {
                            cell.Value = numVal;
                            cell.Style.NumberFormat.Format = "#,##0";
                            cell.Style.Font.Bold = true;
                        }
                        else
                        {
                            cell.Value = valStr;
                        }

                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                        cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        cell.Style.Border.OutsideBorderColor = XLColor.FromHtml("#E2E8F0");
                    }

                    if (dataRowsCount % 2 == 1)
                    {
                        ws.Range(rowIdx, 1, rowIdx, colCount).Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");
                    }
                    ws.Row(rowIdx).Height = 22;
                    rowIdx++;
                    dataRowsCount++;
                }

                WriteSummaryFooter(ws, summary, colCount, ref rowIdx);
                AutoFitArabicColumns(ws, colCount);

                wb.SaveAs(dlg.FileName);
                MessageBox.Show($"تم حفظ وتصدير ({title}) إلى ملف إكسل منسق بنجاح.", "نجاح التصدير", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                AppLogger.LogError($"Excel Export Error: {ex.Message}", ex);
                MessageBox.Show($"حدث خطأ أثناء حفظ ملف الإكسل: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static void PrintGroupedReport(
            string title,
            string subtitle,
            System.Collections.IEnumerable groups,
            Func<object, string> groupHeaderSelector,
            Func<object, System.Collections.IEnumerable> itemsSelector,
            string[] columnHeaders,
            Func<object, string[]> getRowData,
            string summary = "",
            bool preferPdf = false)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(title)) title = "تقرير_مفصل";
                var dlg = new SaveFileDialog
                {
                    Filter = "ملف إكسل منسق (*.xlsx)|*.xlsx",
                    DefaultExt = ".xlsx",
                    FileName = $"{SanitizeFileName(title)}_{DateTime.Now.ToString("yyyyMMdd_HHmm", CultureInfo.InvariantCulture)}.xlsx"
                };

                if (dlg.ShowDialog() != true) return;

                using var wb = new XLWorkbook();
                var ws = wb.Worksheets.Add("التقرير المفصل");
                ws.RightToLeft = true;

                int colCount = columnHeaders.Length;
                int rowIdx = 1;
                StyleTitleAndSummaryBanner(ws, title, subtitle, colCount, ref rowIdx);

                foreach (var grp in groups)
                {
                    if (grp == null) continue;
                    string grpHeader = groupHeaderSelector(grp) ?? "";

                    // Group Banner Row
                    ws.Cell(rowIdx, 1).Value = grpHeader;
                    ws.Range(rowIdx, 1, rowIdx, colCount).Merge();
                    var grpRange = ws.Range(rowIdx, 1, rowIdx, colCount);
                    grpRange.Style.Font.Bold = true;
                    grpRange.Style.Font.FontSize = 12;
                    grpRange.Style.Font.FontColor = XLColor.FromHtml("#00358C");
                    grpRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#DBEAFE");
                    grpRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                    grpRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    ws.Row(rowIdx).Height = 25;
                    rowIdx++;

                    // Table Headers for Group
                    for (int c = 0; c < colCount; c++)
                    {
                        var cell = ws.Cell(rowIdx, c + 1);
                        cell.Value = columnHeaders[c];
                        cell.Style.Font.Bold = true;
                        cell.Style.Font.FontSize = 11;
                        cell.Style.Font.FontColor = XLColor.White;
                        cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E3A8A");
                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                        cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        cell.Style.Border.OutsideBorderColor = XLColor.FromHtml("#CBD5E1");
                    }
                    ws.Row(rowIdx).Height = 24;
                    rowIdx++;

                    var items = itemsSelector(grp);
                    int itemIdx = 0;
                    if (items != null)
                    {
                        foreach (var item in items)
                        {
                            if (item == null) continue;
                            var rowValues = getRowData(item);
                            if (rowValues == null) continue;

                            for (int c = 0; c < colCount && c < rowValues.Length; c++)
                            {
                                var cell = ws.Cell(rowIdx, c + 1);
                                string valStr = rowValues[c] ?? "";
                                if (TryParseNumericCell(valStr, out decimal numVal))
                                {
                                    cell.Value = numVal;
                                    cell.Style.NumberFormat.Format = "#,##0";
                                    cell.Style.Font.Bold = true;
                                }
                                else
                                {
                                    cell.Value = valStr;
                                }
                                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                                cell.Style.Border.OutsideBorderColor = XLColor.FromHtml("#E2E8F0");
                            }

                            if (itemIdx % 2 == 1)
                            {
                                ws.Range(rowIdx, 1, rowIdx, colCount).Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");
                            }
                            ws.Row(rowIdx).Height = 22;
                            rowIdx++;
                            itemIdx++;
                        }
                    }
                    rowIdx++;
                }

                WriteSummaryFooter(ws, summary, colCount, ref rowIdx);
                AutoFitArabicColumns(ws, colCount);

                wb.SaveAs(dlg.FileName);
                MessageBox.Show($"تم حفظ وتصدير ({title}) إلى ملف إكسل منسق بنجاح.", "نجاح التصدير", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء حفظ ملف الإكسل: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static void PrintMultiSectionReport(
            string title,
            string subtitle,
            List<ReportSection> sections,
            string finalSummary = "",
            bool preferPdf = false)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(title)) title = "التقرير_العام_الشامل";
                var dlg = new SaveFileDialog
                {
                    Filter = "ملف إكسل منسق (*.xlsx)|*.xlsx",
                    DefaultExt = ".xlsx",
                    FileName = $"{SanitizeFileName(title)}_{DateTime.Now.ToString("yyyyMMdd_HHmm", CultureInfo.InvariantCulture)}.xlsx"
                };

                if (dlg.ShowDialog() != true) return;

                using var wb = new XLWorkbook();
                var ws = wb.Worksheets.Add("التقرير الشامل");
                ws.RightToLeft = true;

                int maxCols = sections
                    .Where(s => s.ColumnHeaders != null && s.ColumnHeaders.Length > 0)
                    .Select(s => s.ColumnHeaders.Length)
                    .DefaultIfEmpty(6)
                    .Max();
                maxCols = Math.Max(maxCols, 6);

                int rowIdx = 1;
                StyleTitleAndSummaryBanner(ws, title, subtitle, maxCols, ref rowIdx);

                foreach (var sec in sections)
                {
                    if (sec == null) continue;
                    int secCols = (sec.ColumnHeaders != null && sec.ColumnHeaders.Length > 0) ? sec.ColumnHeaders.Length : maxCols;

                    if (!string.IsNullOrWhiteSpace(sec.Title))
                    {
                        ws.Cell(rowIdx, 1).Value = sec.Title;
                        ws.Range(rowIdx, 1, rowIdx, secCols).Merge();
                        var secRange = ws.Range(rowIdx, 1, rowIdx, secCols);
                        secRange.Style.Font.Bold = true;
                        secRange.Style.Font.FontSize = 13;
                        secRange.Style.Font.FontColor = XLColor.FromHtml("#00358C");
                        secRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#DBEAFE");
                        secRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                        secRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                        ws.Row(rowIdx).Height = 26;
                        rowIdx++;
                    }

                    if (sec.SummaryItems != null && sec.SummaryItems.Any())
                    {
                        foreach (var sItem in sec.SummaryItems)
                        {
                            if (string.IsNullOrWhiteSpace(sItem) || sItem.Trim() == "---") continue;
                            ws.Cell(rowIdx, 1).Value = $"• {sItem.Trim()}";
                            ws.Range(rowIdx, 1, rowIdx, secCols).Merge();
                            var itemRange = ws.Range(rowIdx, 1, rowIdx, secCols);
                            itemRange.Style.Font.Bold = true;
                            itemRange.Style.Font.FontSize = 11;
                            itemRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");
                            itemRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                            itemRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                            for (int c = 1; c <= secCols; c++)
                            {
                                ws.Cell(rowIdx, c).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                                ws.Cell(rowIdx, c).Style.Border.OutsideBorderColor = XLColor.FromHtml("#E2E8F0");
                            }
                            ws.Row(rowIdx).Height = 22;
                            rowIdx++;
                        }
                        rowIdx++;
                    }

                    if (sec.ColumnHeaders != null && sec.ColumnHeaders.Length > 0 && sec.Rows != null && sec.Rows.Any())
                    {
                        for (int c = 0; c < sec.ColumnHeaders.Length; c++)
                        {
                            var cell = ws.Cell(rowIdx, c + 1);
                            cell.Value = sec.ColumnHeaders[c];
                            cell.Style.Font.Bold = true;
                            cell.Style.Font.FontSize = 11;
                            cell.Style.Font.FontColor = XLColor.White;
                            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E3A8A");
                            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                            cell.Style.Border.OutsideBorderColor = XLColor.FromHtml("#CBD5E1");
                        }
                        ws.Row(rowIdx).Height = 25;
                        rowIdx++;

                        int rCount = 0;
                        foreach (var rData in sec.Rows)
                        {
                            if (rData == null) continue;
                            for (int c = 0; c < sec.ColumnHeaders.Length && c < rData.Length; c++)
                            {
                                var cell = ws.Cell(rowIdx, c + 1);
                                string valStr = rData[c] ?? "";
                                if (TryParseNumericCell(valStr, out decimal numVal))
                                {
                                    cell.Value = numVal;
                                    cell.Style.NumberFormat.Format = "#,##0";
                                    cell.Style.Font.Bold = true;
                                }
                                else
                                {
                                    cell.Value = valStr;
                                }
                                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                                cell.Style.Border.OutsideBorderColor = XLColor.FromHtml("#E2E8F0");
                            }

                            if (rCount % 2 == 1)
                            {
                                ws.Range(rowIdx, 1, rowIdx, sec.ColumnHeaders.Length).Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");
                            }
                            ws.Row(rowIdx).Height = 22;
                            rowIdx++;
                            rCount++;
                        }
                        rowIdx++;
                    }
                }

                WriteSummaryFooter(ws, finalSummary, maxCols, ref rowIdx);
                AutoFitArabicColumns(ws, maxCols);

                wb.SaveAs(dlg.FileName);
                MessageBox.Show($"تم حفظ وتصدير ({title}) إلى ملف إكسل منسق بنجاح.", "نجاح التصدير", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء حفظ ملف الإكسل: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public class ReportSection
    {
        public string Title { get; set; }
        public List<string> SummaryItems { get; set; } = new List<string>();
        public string[] ColumnHeaders { get; set; }
        public List<string[]> Rows { get; set; } = new List<string[]>();
    }
}
