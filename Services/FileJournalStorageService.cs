using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using JournalApp.Models;

namespace JournalApp.Services
{
    public class FileJournalStorageService : IJournalStorageService
    {
        private readonly string _filePath;
        private readonly string _settingsPath;
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
        };

        public FileJournalStorageService()
        {
            var dir = FileSystem.AppDataDirectory;
            _filePath = Path.Combine(dir, "entries.json");
            _settingsPath = Path.Combine(dir, "settings.json");
            EnsureSettings();
        }

        private async Task<List<JournalEntry>> ReadAllAsync()
        {
            try
            {
                if (!File.Exists(_filePath))
                    return new List<JournalEntry>();

                using var stream = File.OpenRead(_filePath);
                var entries = await JsonSerializer.DeserializeAsync<List<JournalEntry>>(stream, _jsonOptions);
                return entries ?? new List<JournalEntry>();
            }
            catch
            {
                return new List<JournalEntry>();
            }
        }

        private async Task WriteAllAsync(List<JournalEntry> entries)
        {
            var dir = Path.GetDirectoryName(_filePath) ?? FileSystem.AppDataDirectory;
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            using var stream = File.Create(_filePath);
            await JsonSerializer.SerializeAsync(stream, entries, _jsonOptions);
        }

        public async Task<List<JournalEntry>> GetEntriesAsync()
        {
            return (await ReadAllAsync()).OrderByDescending(e => e.Date).ToList();
        }

        public async Task<JournalEntry?> GetEntryAsync(Guid id)
        {
            var entries = await ReadAllAsync();
            return entries.FirstOrDefault(e => e.Id == id);
        }

        public async Task SaveEntryAsync(JournalEntry entry)
        {
            var entries = await ReadAllAsync();
            entry.CreatedAt = DateTime.UtcNow;
            entry.UpdatedAt = DateTime.UtcNow;
            entries.Add(entry);
            await WriteAllAsync(entries);
        }

        public async Task UpdateEntryAsync(JournalEntry entry)
        {
            var entries = await ReadAllAsync();
            var idx = entries.FindIndex(e => e.Id == entry.Id);
            if (idx >= 0)
            {
                entry.UpdatedAt = DateTime.UtcNow;
                entries[idx] = entry;
                await WriteAllAsync(entries);
            }
        }

        public async Task DeleteEntryAsync(Guid id)
        {
            var entries = await ReadAllAsync();
            entries.RemoveAll(e => e.Id == id);
            await WriteAllAsync(entries);
        }

        public async Task ExportAsync(string filePath)
        {
            var entries = await ReadAllAsync();
            using var stream = File.Create(filePath);
            await JsonSerializer.SerializeAsync(stream, entries, _jsonOptions);
        }

        public async Task ImportAsync(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Import file not found", filePath);

            using var stream = File.OpenRead(filePath);
            var imported = await JsonSerializer.DeserializeAsync<List<JournalEntry>>(stream, _jsonOptions);
            if (imported == null) return;

            // Merge: keep existing entries and add any new ones (by Id)
            var existing = await ReadAllAsync();
            var existingIds = existing.Select(e => e.Id).ToHashSet();
            var toAdd = imported.Where(e => !existingIds.Contains(e.Id)).ToList();
            existing.AddRange(toAdd);
            await WriteAllAsync(existing);
        }

        public async Task ClearAllAsync()
        {
            await WriteAllAsync(new List<JournalEntry>());
        }

        // Settings management
        private class SettingsData
        {
            public List<string> Tags { get; set; } = new();
            public List<string> Moods { get; set; } = new();
        }

        private void EnsureSettings()
        {
            try
            {
                if (!File.Exists(_settingsPath))
                {
                    var defaults = new SettingsData
                    {
                        Tags = new List<string> { "Self-Care", "Exercise", "Work", "Family", "Friends", "Travel", "Learning" },
                        Moods = new List<string> { "Happy", "Sad", "Anxious", "Angry", "Relaxed", "Excited", "Neutral", "Content" }
                    };
                    var dir = Path.GetDirectoryName(_settingsPath) ?? FileSystem.AppDataDirectory;
                    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                    using var s = File.Create(_settingsPath);
                    JsonSerializer.Serialize(s, defaults, _jsonOptions);
                }
            }
            catch { }
        }

        private SettingsData ReadSettings()
        {
            try
            {
                if (!File.Exists(_settingsPath)) return new SettingsData();
                var json = File.ReadAllText(_settingsPath);
                var data = JsonSerializer.Deserialize<SettingsData>(json, _jsonOptions);
                return data ?? new SettingsData();
            }
            catch
            {
                return new SettingsData();
            }
        }

        private void WriteSettings(SettingsData data)
        {
            var dir = Path.GetDirectoryName(_settingsPath) ?? FileSystem.AppDataDirectory;
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(_settingsPath, JsonSerializer.Serialize(data, _jsonOptions));
        }

        public Task<List<string>> GetCustomTagsAsync()
        {
            var s = ReadSettings();
            return Task.FromResult(s.Tags.ToList());
        }

        public Task AddCustomTagAsync(string tag)
        {
            var s = ReadSettings();
            if (!s.Tags.Contains(tag)) s.Tags.Add(tag);
            WriteSettings(s);
            return Task.CompletedTask;
        }

        public Task RemoveCustomTagAsync(string tag)
        {
            var s = ReadSettings();
            s.Tags.RemoveAll(t => t == tag);
            WriteSettings(s);
            return Task.CompletedTask;
        }

        public Task<List<string>> GetCustomMoodsAsync()
        {
            var s = ReadSettings();
            return Task.FromResult(s.Moods.ToList());
        }

        public Task AddCustomMoodAsync(string mood)
        {
            var s = ReadSettings();
            if (!s.Moods.Contains(mood)) s.Moods.Add(mood);
            WriteSettings(s);
            return Task.CompletedTask;
        }

        public Task RemoveCustomMoodAsync(string mood)
        {
            var s = ReadSettings();
            s.Moods.RemoveAll(m => m == mood);
            WriteSettings(s);
            return Task.CompletedTask;
        }

    }
}
