using ClosedXML.Excel;
using System.Linq;

namespace eSchool.Infrastructure
{
    public static class ExcelHelper
    {
        public static void ApplyTemplateStyle(IXLWorksheet worksheet, int headerRowIndex = 1)
        {
            var lastCol = worksheet.LastColumnUsed()?.ColumnNumber() ?? 1;
            var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? headerRowIndex;

            // 1. Header range
            var headerRange = worksheet.Range(headerRowIndex, 1, headerRowIndex, lastCol);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Font.FontColor = XLColor.White;
            headerRange.Style.Font.FontSize = 11;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1f4e78");
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            headerRange.Style.Border.OutsideBorderColor = XLColor.FromHtml("#1f4e78");
            headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            headerRange.Style.Border.InsideBorderColor = XLColor.FromHtml("#1f4e78");

            worksheet.Row(headerRowIndex).Height = 26;

            // 2. Data range (apply light green style to data or template placeholder rows)
            int dataEndRow = lastRow > headerRowIndex ? lastRow : headerRowIndex + 3;
            var dataRange = worksheet.Range(headerRowIndex + 1, 1, dataEndRow, lastCol);
            dataRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#d9ead3");
            dataRange.Style.Font.FontColor = XLColor.Black;
            dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            dataRange.Style.Border.OutsideBorderColor = XLColor.FromHtml("#8fce00");
            dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            dataRange.Style.Border.InsideBorderColor = XLColor.FromHtml("#8fce00");
            dataRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            for (int r = headerRowIndex + 1; r <= dataEndRow; r++)
            {
                worksheet.Row(r).Height = 22;
            }

            worksheet.Columns(1, lastCol).AdjustToContents(15.0, 50.0);
        }
    }
}
