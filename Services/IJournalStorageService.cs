using System.Text.Json;
using JournalApp.Models;

namespace JournalApp.Services
{
    public class IJournalStorageService
    {
        // --- FILE PATHS ---
        private const string EntriesFile = "entries.json";
        private const string TagsFile = "tags.json";
        private const string MoodsFile = "moods.json";

        // --- ENTRIES ---
        public async Task<List<JournalEntry>> GetEntriesAsync()
        {
            if (!File.Exists(EntriesFile))
                return new List<JournalEntry>();

            var json = await File.ReadAllTextAsync(EntriesFile);
            return JsonSerializer.Deserialize<List<JournalEntry>>(json) ?? new List<JournalEntry>();
        }

        public async Task<JournalEntry?> GetEntryAsync(Guid id)
        {
            var entries = await GetEntriesAsync();
            return entries.FirstOrDefault(e => e.Id == id);
        }

        public async Task SaveEntryAsync(JournalEntry entry)
        {
            var entries = await GetEntriesAsync();
            entries.Add(entry);
            await SaveAll(entries);
        }

        public async Task UpdateEntryAsync(JournalEntry entry)
        {
            var entries = await GetEntriesAsync();
            var index = entries.FindIndex(e => e.Id == entry.Id);
            if (index >= 0)
                entries[index] = entry;
            await SaveAll(entries);
        }

        public async Task DeleteEntryAsync(Guid id)
        {
            var entries = await GetEntriesAsync();
            entries.RemoveAll(e => e.Id == id);
            await SaveAll(entries);
        }

        private async Task SaveAll(List<JournalEntry> entries)
        {
            var json = JsonSerializer.Serialize(entries, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(EntriesFile, json);
        }

        public async Task<bool> HasJournalForDateAsync(DateTime date)
        {
            var entries = await GetEntriesAsync();
            return entries.Any(e => e.CreatedAt.Date == date.Date);
        }

        public async Task ExportAsync(string filePath)
        {
            var entries = await GetEntriesAsync();
            var json = JsonSerializer.Serialize(entries, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(filePath, json);
        }

        public async Task ClearAllAsync()
        {
            if (File.Exists(EntriesFile))
                File.Delete(EntriesFile);
        }

        // --- CUSTOM TAGS ---
        public async Task<List<string>> GetCustomTagsAsync()
        {
            if (!File.Exists(TagsFile))
                return new List<string>();
            var json = await File.ReadAllTextAsync(TagsFile);
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }

        public async Task AddCustomTagAsync(string tag)
        {
            var tags = await GetCustomTagsAsync();
            if (!tags.Contains(tag))
            {
                tags.Add(tag);
                await SaveTagsAsync(tags);
            }
        }

        public async Task RemoveCustomTagAsync(string tag)
        {
            var tags = await GetCustomTagsAsync();
            tags.Remove(tag);
            await SaveTagsAsync(tags);
        }

        public async Task SaveTagsAsync(List<string> tags)
        {
            var json = JsonSerializer.Serialize(tags, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(TagsFile, json);
        }

        // --- CUSTOM MOODS ---
        public async Task<List<string>> GetCustomMoodsAsync()
        {
            if (!File.Exists(MoodsFile))
                return new List<string>();
            var json = await File.ReadAllTextAsync(MoodsFile);
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }

        public async Task AddCustomMoodAsync(string mood)
        {
            var moods = await GetCustomMoodsAsync();
            if (!moods.Contains(mood))
            {
                moods.Add(mood);
                await SaveMoodsAsync(moods);
            }
        }

        public async Task RemoveCustomMoodAsync(string mood)
        {
            var moods = await GetCustomMoodsAsync();
            moods.Remove(mood);
            await SaveMoodsAsync(moods);
        }

       public async Task SaveMoodsAsync(List<string> moods)
        {
            var json = JsonSerializer.Serialize(moods, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(MoodsFile, json);
        }
    }
}
