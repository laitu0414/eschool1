using System.Globalization;
using ClosedXML.Excel;

namespace eSchool.Services;

public sealed record DiemExcelRow(int RowNumber, int StudentId, string StudentCode, string?[] Grades);

public static class DiemExcelReader
{
    public static readonly string[] Headers =
    {
        "ID Học sinh (*Không sửa*)", "Mã Học sinh", "Họ Tên",
        "Điểm 15 Phút", "Điểm 1 Tiết", "Điểm Giữa Kỳ", "Điểm Cuối Kỳ"
    };

    public static List<DiemExcelRow> Read(XLWorkbook workbook, List<string> errors)
    {
        if (!workbook.TryGetWorksheet("NhapDiem", out var sheet))
        {
            errors.Add("Không tìm thấy trang NhapDiem. Hãy sử dụng file mẫu.");
            return new();
        }
        for (var column = 1; column <= Headers.Length; column++)
            if (sheet.Cell(1, column).GetString().Trim() != Headers[column - 1])
                errors.Add($"Cột {column}: tiêu đề phải là '{Headers[column - 1]}'.");
        if (errors.Count > 0) return new();
        var lastRow = sheet.LastRowUsed(XLCellsUsedOptions.Contents)?.RowNumber() ?? 1;
        if (lastRow > 2001)
        {
            errors.Add("File vượt quá 2.000 dòng học sinh.");
            return new();
        }
        var result = new List<DiemExcelRow>();
        var seen = new HashSet<int>();
        for (var row = 2; row <= lastRow; row++)
        {
            if (Enumerable.Range(1, 7).All(c => sheet.Cell(row, c).IsEmpty())) continue;
            var idCell = sheet.Cell(row, 1);
            if (idCell.HasFormula || !int.TryParse(idCell.GetString(), out var id) || id <= 0)
            {
                errors.Add($"Dòng {row}: ID học sinh không hợp lệ.");
                continue;
            }
            if (!seen.Add(id)) errors.Add($"Dòng {row}: học sinh ID {id} bị lặp.");
            var codeCell = sheet.Cell(row, 2);
            if (codeCell.HasFormula || string.IsNullOrWhiteSpace(codeCell.GetString()))
                errors.Add($"Dòng {row}: mã học sinh không hợp lệ.");
            var grades = new string?[4];
            for (var column = 4; column <= 7; column++)
            {
                try { grades[column - 4] = ReadGrade(sheet.Cell(row, column)); }
                catch (FormatException ex) { errors.Add($"Dòng {row}, {Headers[column - 1]}: {ex.Message}"); }
            }
            result.Add(new(row, id, codeCell.GetString().Trim(), grades));
        }
        return result;
    }

    public static string? ReadGrade(IXLCell cell)
    {
        if (cell.HasFormula) throw new FormatException("Hãy nhập giá trị điểm, không dùng công thức.");
        if (cell.IsEmpty() || (cell.DataType == XLDataType.Text && string.IsNullOrWhiteSpace(cell.GetString())))
            return null;
        if (cell.DataType != XLDataType.Number && cell.DataType != XLDataType.Text)
            throw new FormatException("Điểm phải là số từ 0 đến 10.");
        var raw = cell.DataType == XLDataType.Number
            ? cell.GetDouble().ToString("G", CultureInfo.InvariantCulture)
            : cell.GetString().Trim();
        return NormalizeInput(raw);
    }

    public static string? NormalizeInput(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var values = new List<string>();
        foreach (var part in raw.Split(';'))
        {
            var token = part.Trim().Replace(',', '.');
            if (!decimal.TryParse(token, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var value) ||
                value < 0 || value > 10)
                throw new FormatException("Điểm phải từ 0 đến 10; ngăn cách nhiều điểm bằng dấu chấm phẩy (ví dụ 8; 7,5).");
            values.Add(value.ToString("0.############################", CultureInfo.InvariantCulture));
        }
        var normalized = string.Join(",", values);
        if (normalized.Length > 255) throw new FormatException("Danh sách điểm quá dài (tối đa 255 ký tự).");
        return normalized;
    }
}