using JournalApp.Data;
using JournalApp.Models;
using Microsoft.EntityFrameworkCore;

namespace JournalApp.Services;

public class AnalyticsService
{
    private readonly JournalDbContext _context;

    public AnalyticsService(JournalDbContext context)
    {
        _context = context;
    }

    public async Task<AnalyticsData> GetAnalyticsAsync(DateTime? startDate, DateTime? endDate)
    {
        var entries = await _context.JournalEntries.ToListAsync();

        //  DATE FILTERING (SAFE)
        if (startDate.HasValue)
            entries = entries.Where(e => e.CreatedAt.Date >= startDate.Value.Date).ToList();

        if (endDate.HasValue)
            entries = entries.Where(e => e.CreatedAt.Date <= endDate.Value.Date).ToList();

        var analytics = new AnalyticsData
        {
            TotalEntries = entries.Count
        };

        // MOOD DISTRIBUTION
        analytics.MoodDistribution = entries
            .GroupBy(e => GetMoodCategory(e.PrimaryMood))
            .ToDictionary(g => g.Key, g => g.Count());

        //  MOST FREQUENT MOOD
        if (analytics.MoodDistribution.Any())
        {
            var topMood = analytics.MoodDistribution
                .OrderByDescending(m => m.Value)
                .First();

            analytics.MostFrequentMood = new MoodInfo
            {
                Name = topMood.Key.ToString(),
                Emoji = GetMoodEmoji(topMood.Key),
                Category = topMood.Key
            };
        }

        // TAG USAGE
        analytics.TagUsage = entries
            .SelectMany(e => e.Tags)
            .GroupBy(t => t)
            .ToDictionary(g => g.Key, g => g.Count());

        // AVERAGE WORD COUNT
        analytics.AverageWordCount = entries.Any()
            ? entries.Average(e => e.Content.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length)
            : 0;

        // CATEGORY BREAKDOWN (using mood category)
        analytics.CategoryBreakdown = entries
            .GroupBy(e => GetMoodCategory(e.PrimaryMood).ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        return analytics;
    }

    private MoodCategory GetMoodCategory(string mood)
    {
        return mood.ToLower() switch
        {
            "happy" or "excited" => MoodCategory.Positive,
            "sad" or "angry" => MoodCategory.Negative,
            _ => MoodCategory.Neutral
        };
    }

    private string GetMoodEmoji(MoodCategory category)
    {
        return category switch
        {
            MoodCategory.Positive => "😊",
            MoodCategory.Neutral => "😐",
            MoodCategory.Negative => "😢",
            _ => "❓"
        };
    }
}
