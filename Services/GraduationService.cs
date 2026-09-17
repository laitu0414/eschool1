namespace eSchool.Services;

public static class GraduationService
{
    // Eligibility and the locked snapshot are checked by the controller before this transition.
    public static bool MarkGraduated(HocSinh student, string academicYear, DateTime confirmedAt)
    {
        if (student.DaTotNghiep)
        {
            if (AnnualScoreService.NormalizeYear(student.NamHocTotNghiep) != AnnualScoreService.NormalizeYear(academicYear))
                throw new InvalidOperationException("Học sinh đã tốt nghiệp ở năm khác.");
            return false;
        }
        if (!student.TrangThai || !student.DaDuyet || AnnualScoreService.ParseGradeLevel(student.LopHoc?.Khoi) != 9 ||
            AnnualScoreService.NormalizeYear(student.LopHoc?.NamHoc) != AnnualScoreService.NormalizeYear(academicYear))
            throw new InvalidOperationException("Học sinh chưa có trạng thái hợp lệ để xác nhận tốt nghiệp.");
        student.DaTotNghiep = true;
        student.NgayTotNghiep = confirmedAt;
        student.NamHocTotNghiep = academicYear;
        student.TrangThai = false;
        return true;
    }
}