using JournalApp.Data;
using Microsoft.EntityFrameworkCore;

namespace JournalApp.Services;

public class StreakService
{
    private readonly JournalDbContext _context;

    public StreakService(JournalDbContext context)
    {
        _context = context;
    }

    // 🔥 CURRENT STREAK
    public async Task<int> GetCurrentStreakAsync()
    {
        var dates = await _context.JournalEntries
            .Select(e => e.CreatedAt.Date)
            .Distinct()
            .OrderByDescending(d => d)
            .ToListAsync();

        if (!dates.Any())
            return 0;

        int streak = 0;
        DateTime expectedDate = DateTime.Today;

        foreach (var date in dates)
        {
            if (date == expectedDate)
            {
                streak++;
                expectedDate = expectedDate.AddDays(-1);
            }
            else
            {
                break;
            }
        }

        return streak;
    }

    // 🏆 LONGEST STREAK
    public async Task<int> GetLongestStreakAsync()
    {
        var dates = await _context.JournalEntries
            .Select(e => e.CreatedAt.Date)
            .Distinct()
            .OrderBy(d => d)
            .ToListAsync();

        if (!dates.Any())
            return 0;

        int longest = 1;
        int current = 1;

        for (int i = 1; i < dates.Count; i++)
        {
            if (dates[i] == dates[i - 1].AddDays(1))
                current++;
            else
                current = 1;

            longest = Math.Max(longest, current);
        }

        return longest;
    }
}
