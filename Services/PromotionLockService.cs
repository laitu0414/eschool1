using eSchool.Models;
using Microsoft.EntityFrameworkCore;
namespace eSchool.Services;

public static class PromotionLockService
{
    public static string Key(int yearId) => $"LenLop.ResultsLocked:{yearId}";
    public static bool IsLocked(AppDbContext context, int? yearId)
    {
        if (!yearId.HasValue) return false;
        var key = Key(yearId.Value);
        var state = context.NhatKyHoatDongs.AsNoTracking().Where(l => l.HanhDong == key)
            .OrderByDescending(l => l.IdNhatKy).Select(l => l.NoiDung).FirstOrDefault();
        // Preserve existing locks until an administrator explicitly unlocks the selected year.
        state ??= context.NhatKyHoatDongs.AsNoTracking().Where(l => l.HanhDong == "LenLop.ResultsLocked")
            .OrderByDescending(l => l.IdNhatKy).Select(l => l.NoiDung).FirstOrDefault();
        return state == bool.TrueString;
    }
}