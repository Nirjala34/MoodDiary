using System;
using System.Collections.Generic;

namespace JournalApp.Models
{
    public class JournalEntry
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;

        public string PrimaryMood { get; set; } = string.Empty;
         public bool IsLocked { get; set; }
        public DateTime? LockedAt { get; set; }
        public List<string> SecondaryMoods { get; set; } = new();
         public string SecondaryMood1 { get; set; } = string.Empty;
         
    public string SecondaryMood2 { get; set; } = string.Empty;

        public string MoodComment { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new();
          public string OptionalNote { get; set; }   = "";

        // SINGLE SOURCE OF DATE TRUTH
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // ✅ Add this alias property so all code using `.Date` works
        public DateTime Date
        {
            get => CreatedAt;
            set => CreatedAt = value;
        }
    }

    public enum MoodCategory
    {
        Positive,
        Neutral,
        Negative
    }

    public class MoodInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Emoji { get; set; } = string.Empty;
        public MoodCategory Category { get; set; }
    }

    public class AnalyticsData
    {
        public int TotalEntries { get; set; }
        public Dictionary<MoodCategory, int> MoodDistribution { get; set; } = new();
        public MoodInfo? MostFrequentMood { get; set; }
        public Dictionary<string, int> TagUsage { get; set; } = new();
        public double AverageWordCount { get; set; }
        public int CurrentStreak { get; set; }
       

        public Dictionary<string, int> CategoryBreakdown { get; set; } = new();
    }
}
