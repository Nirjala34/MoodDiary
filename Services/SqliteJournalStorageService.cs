using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using JournalApp.Models;
using Microsoft.Data.Sqlite;

namespace JournalApp.Services
{
    public class SqliteJournalStorageService : IJournalStorageService
    {
        private readonly string _dbPath;
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { WriteIndented = true };

        public SqliteJournalStorageService()
        {
            var dir = FileSystem.AppDataDirectory;
            _dbPath = Path.Combine(dir, "journal.db");
            EnsureDatabase();
            TryMigrateFromJson();
        }

        private void EnsureDatabase()
        {
            var dir = Path.GetDirectoryName(_dbPath) ?? FileSystem.AppDataDirectory;
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            using var conn = GetConnection();
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS entries (
                    Id TEXT PRIMARY KEY,
                    Date TEXT,
                    Title TEXT,
                    Content TEXT,
                    PrimaryMood TEXT,
                    SecondaryMoodsJson TEXT,
                    MoodComment TEXT,
                    TagsJson TEXT,
                    CreatedAt TEXT,
                    UpdatedAt TEXT,
                    IsLocked INTEGER DEFAULT 0,
                    LockedAt TEXT,
                    OptionalNote TEXT
                );";
            cmd.ExecuteNonQuery();

            // Safely add columns if they don't exist (for existing users)
            AddColumnIfMissing(conn, "IsLocked", "INTEGER DEFAULT 0");
            AddColumnIfMissing(conn, "LockedAt", "TEXT");
            AddColumnIfMissing(conn, "OptionalNote", "TEXT");

            // Settings table
            using var scmd = conn.CreateCommand();
            scmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS settings (
                    Key TEXT PRIMARY KEY,
                    Value TEXT
                );";
            scmd.ExecuteNonQuery();
        }

        private void AddColumnIfMissing(SqliteConnection conn, string columnName, string columnType)
        {
            try
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = $"SELECT count(*) FROM pragma_table_info('entries') WHERE name = '{columnName}'";
                var count = Convert.ToInt32(cmd.ExecuteScalar() ?? 0);
                if (count == 0)
                {
                    using var alterCmd = conn.CreateCommand();
                    alterCmd.CommandText = $"ALTER TABLE entries ADD COLUMN {columnName} {columnType}";
                    alterCmd.ExecuteNonQuery();
                }
            }
            catch { /* ignore migration errors for single columns */ }
        }

        private SqliteConnection GetConnection() => new SqliteConnection($"Data Source={_dbPath}");

        private void TryMigrateFromJson()
        {
            try
            {
                var entriesJsonPath = Path.Combine(FileSystem.AppDataDirectory, "entries.json");
                var settingsJsonPath = Path.Combine(FileSystem.AppDataDirectory, "settings.json");

                using var conn = GetConnection();
                conn.Open();
                using var countCmd = conn.CreateCommand();
                countCmd.CommandText = "SELECT COUNT(1) FROM entries";
                var hasRows = Convert.ToInt32(countCmd.ExecuteScalar() ?? 0) > 0;

                // Migrate entries.json -> entries table (only if empty)
                if (!hasRows && File.Exists(entriesJsonPath))
                {
                    var json = File.ReadAllText(entriesJsonPath);
                    var imported = JsonSerializer.Deserialize<List<JournalEntry>>(json, _jsonOptions);
                    if (imported != null && imported.Count > 0)
                    {
                        foreach (var entry in imported)
                        {
                            InsertEntry(entry);
                        }
                    }

                    // Delete legacy file after successful migration
                    try { File.Delete(entriesJsonPath); } catch { }
                }

                // Migrate settings.json -> settings table
                if (File.Exists(settingsJsonPath))
                {
                    try
                    {
                        var json = File.ReadAllText(settingsJsonPath);
                        var data = JsonSerializer.Deserialize<SettingsData>(json, _jsonOptions);
                        if (data != null)
                        {
                            WriteSettings("tags", JsonSerializer.Serialize(data.Tags ?? new List<string>(), _jsonOptions));
                            WriteSettings("moods", JsonSerializer.Serialize(data.Moods ?? new List<string>(), _jsonOptions));
                        }

                        try { File.Delete(settingsJsonPath); } catch { }
                    }
                    catch { /* ignore settings migration errors */ }
                }
            }
            catch { /* swallow - migration should not crash app */ }
        }

        private void InsertEntry(JournalEntry entry)
        {
            using var conn = GetConnection();
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT OR REPLACE INTO entries (Id, Date, Title, Content, PrimaryMood, SecondaryMoodsJson, MoodComment, TagsJson, CreatedAt, UpdatedAt, IsLocked, LockedAt, OptionalNote)
                VALUES ($id, $date, $title, $content, $primary, $secondaryJson, $moodComment, $tagsJson, $created, $updated, $isLocked, $lockedAt, $optionalNote);";
            cmd.Parameters.AddWithValue("$id", entry.Id.ToString());
            cmd.Parameters.AddWithValue("$date", entry.Date.ToString("o"));
            cmd.Parameters.AddWithValue("$title", entry.Title ?? string.Empty);
            cmd.Parameters.AddWithValue("$content", entry.Content ?? string.Empty);
            cmd.Parameters.AddWithValue("$primary", entry.PrimaryMood ?? string.Empty);
            cmd.Parameters.AddWithValue("$secondaryJson", JsonSerializer.Serialize(entry.SecondaryMoods ?? new List<string>(), _jsonOptions));
            cmd.Parameters.AddWithValue("$moodComment", entry.MoodComment ?? string.Empty);
            cmd.Parameters.AddWithValue("$tagsJson", JsonSerializer.Serialize(entry.Tags ?? new List<string>(), _jsonOptions));
            cmd.Parameters.AddWithValue("$created", entry.CreatedAt.ToString("o"));
            cmd.Parameters.AddWithValue("$updated", entry.UpdatedAt.ToString("o"));
            cmd.Parameters.AddWithValue("$isLocked", entry.IsLocked ? 1 : 0);
            cmd.Parameters.AddWithValue("$lockedAt", entry.LockedAt?.ToString("o") ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("$optionalNote", entry.OptionalNote ?? string.Empty);
            cmd.ExecuteNonQuery();
        }

        public async Task<List<JournalEntry>> GetEntriesAsync()
        {
            var list = new List<JournalEntry>();
            using var conn = GetConnection();
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM entries ORDER BY UpdatedAt DESC";
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(ReadEntry(reader));
            }
            return list.OrderByDescending(e => e.Date).ToList();
        }

        public async Task<JournalEntry?> GetEntryAsync(Guid id)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM entries WHERE Id = $id LIMIT 1";
            cmd.Parameters.AddWithValue("$id", id.ToString());
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync()) return ReadEntry(reader);
            return null;
        }

        public async Task SaveEntryAsync(JournalEntry entry)
        {
            entry.UpdatedAt = DateTime.UtcNow;
            InsertEntry(entry);
            await Task.CompletedTask;
        }

        public async Task<bool> HasJournalForDateAsync(DateTime date)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(1) FROM entries WHERE substr(Date, 1, 10) = $date";
            cmd.Parameters.AddWithValue("$date", date.ToString("yyyy-MM-dd"));
            var count = Convert.ToInt32(await cmd.ExecuteScalarAsync() ?? 0);
            return count > 0;
        }

        public async Task UpdateEntryAsync(JournalEntry entry)
        {
            entry.UpdatedAt = DateTime.UtcNow;
            using var conn = GetConnection();
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE entries SET
                    Date = $date,
                    Title = $title,
                    Content = $content,
                    PrimaryMood = $primary,
                    SecondaryMoodsJson = $secondaryJson,
                    MoodComment = $moodComment,
                    TagsJson = $tagsJson,
                    UpdatedAt = $updated,
                    IsLocked = $isLocked,
                    LockedAt = $lockedAt,
                    OptionalNote = $optionalNote
                WHERE Id = $id;";
            cmd.Parameters.AddWithValue("$id", entry.Id.ToString());
            cmd.Parameters.AddWithValue("$date", entry.Date.ToString("o"));
            cmd.Parameters.AddWithValue("$title", entry.Title ?? string.Empty);
            cmd.Parameters.AddWithValue("$content", entry.Content ?? string.Empty);
            cmd.Parameters.AddWithValue("$primary", entry.PrimaryMood ?? string.Empty);
            cmd.Parameters.AddWithValue("$secondaryJson", JsonSerializer.Serialize(entry.SecondaryMoods ?? new List<string>(), _jsonOptions));
            cmd.Parameters.AddWithValue("$moodComment", entry.MoodComment ?? string.Empty);
            cmd.Parameters.AddWithValue("$tagsJson", JsonSerializer.Serialize(entry.Tags ?? new List<string>(), _jsonOptions));
            cmd.Parameters.AddWithValue("$updated", entry.UpdatedAt.ToString("o"));
            cmd.Parameters.AddWithValue("$isLocked", entry.IsLocked ? 1 : 0);
            cmd.Parameters.AddWithValue("$lockedAt", entry.LockedAt?.ToString("o") ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("$optionalNote", entry.OptionalNote ?? string.Empty);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task DeleteEntryAsync(Guid id)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM entries WHERE Id = $id";
            cmd.Parameters.AddWithValue("$id", id.ToString());
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task ExportAsync(string filePath)
        {
            var entries = await GetEntriesAsync();
            using var stream = File.Create(filePath);
            await JsonSerializer.SerializeAsync(stream, entries, _jsonOptions);
        }

        public async Task ImportAsync(string filePath)
        {
            if (!File.Exists(filePath)) throw new FileNotFoundException("Import file not found", filePath);
            using var stream = File.OpenRead(filePath);
            var imported = await JsonSerializer.DeserializeAsync<List<JournalEntry>>(stream, _jsonOptions);
            if (imported == null) return;

            var existing = (await GetEntriesAsync()).Select(e => e.Id).ToHashSet();
            var toAdd = imported.Where(e => !existing.Contains(e.Id)).ToList();
            foreach (var e in toAdd) InsertEntry(e);
        }

        public async Task ClearAllAsync()
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM entries";
            await cmd.ExecuteNonQueryAsync();
        }

        // Settings stored in the settings table (JSON values)
        private void WriteSettings(string key, string value)
        {
            using var conn = GetConnection();
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO settings (Key, Value) VALUES ($key, $value)
                ON CONFLICT(Key) DO UPDATE SET Value = $value;";
            cmd.Parameters.AddWithValue("$key", key);
            cmd.Parameters.AddWithValue("$value", value ?? string.Empty);
            cmd.ExecuteNonQuery();
        }

        private string? ReadSettings(string key)
        {
            using var conn = GetConnection();
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Value FROM settings WHERE Key = $key LIMIT 1";
            cmd.Parameters.AddWithValue("$key", key);
            using var reader = cmd.ExecuteReader();
            if (reader.Read()) return reader.IsDBNull(0) ? null : reader.GetString(0);
            return null;
        }

        public Task<List<string>> GetCustomTagsAsync()
        {
            var json = ReadSettings("tags");
            if (string.IsNullOrEmpty(json))
            {
                return Task.FromResult(new List<string> { "Self-Care", "Exercise", "Work", "Family", "Friends", "Travel", "Learning" });
            }
            var list = JsonSerializer.Deserialize<List<string>>(json, _jsonOptions) ?? new List<string>();
            return Task.FromResult(list);
        }

        public Task AddCustomTagAsync(string tag)
        {
            var list = JsonSerializer.Deserialize<List<string>>(ReadSettings("tags") ?? "[]", _jsonOptions) ?? new List<string>();
            if (!list.Contains(tag)) list.Add(tag);
            WriteSettings("tags", JsonSerializer.Serialize(list, _jsonOptions));
            return Task.CompletedTask;
        }

        public Task RemoveCustomTagAsync(string tag)
        {
            var list = JsonSerializer.Deserialize<List<string>>(ReadSettings("tags") ?? "[]", _jsonOptions) ?? new List<string>();
            list.RemoveAll(t => t == tag);
            WriteSettings("tags", JsonSerializer.Serialize(list, _jsonOptions));
            return Task.CompletedTask;
        }

        public Task SaveTagsAsync(List<string> tags)
        {
            WriteSettings("tags", JsonSerializer.Serialize(tags, _jsonOptions));
            return Task.CompletedTask;
        }

        public Task<List<string>> GetCustomMoodsAsync()
        {
            var json = ReadSettings("moods");
            if (string.IsNullOrEmpty(json))
            {
                return Task.FromResult(new List<string> { "Happy", "Sad", "Anxious", "Angry", "Relaxed", "Excited", "Neutral", "Content" });
            }
            var list = JsonSerializer.Deserialize<List<string>>(json, _jsonOptions) ?? new List<string>();
            return Task.FromResult(list);
        }

        public Task AddCustomMoodAsync(string mood)
        {
            var list = JsonSerializer.Deserialize<List<string>>(ReadSettings("moods") ?? "[]", _jsonOptions) ?? new List<string>();
            if (!list.Contains(mood)) list.Add(mood);
            WriteSettings("moods", JsonSerializer.Serialize(list, _jsonOptions));
            return Task.CompletedTask;
        }

        public Task RemoveCustomMoodAsync(string mood)
        {
            var list = JsonSerializer.Deserialize<List<string>>(ReadSettings("moods") ?? "[]", _jsonOptions) ?? new List<string>();
            list.RemoveAll(m => m == mood);
            WriteSettings("moods", JsonSerializer.Serialize(list, _jsonOptions));
            return Task.CompletedTask;
        }

        public Task SaveMoodsAsync(List<string> moods)
        {
            WriteSettings("moods", JsonSerializer.Serialize(moods, _jsonOptions));
            return Task.CompletedTask;
        }

        // Private helper class for migrating legacy settings.json
        private class SettingsData
        {
            public List<string> Tags { get; set; } = new();
            public List<string> Moods { get; set; } = new();
        }

        // Helper to read a single entry from reader
        private JournalEntry ReadEntry(SqliteDataReader reader)
        {
            try 
            {
                var idStr = reader.IsDBNull(reader.GetOrdinal("Id")) ? Guid.NewGuid().ToString() : reader.GetString(reader.GetOrdinal("Id"));
                var id = Guid.Parse(idStr);
                
                var dateStr = reader.IsDBNull(reader.GetOrdinal("Date")) ? DateTime.UtcNow.ToString("o") : reader.GetString(reader.GetOrdinal("Date"));
                var date = DateTime.Parse(dateStr);
                
                var title = reader.IsDBNull(reader.GetOrdinal("Title")) ? string.Empty : reader.GetString(reader.GetOrdinal("Title"));
                var content = reader.IsDBNull(reader.GetOrdinal("Content")) ? string.Empty : reader.GetString(reader.GetOrdinal("Content"));
                var primary = reader.IsDBNull(reader.GetOrdinal("PrimaryMood")) ? string.Empty : reader.GetString(reader.GetOrdinal("PrimaryMood"));
                
                var secondaryJson = reader.IsDBNull(reader.GetOrdinal("SecondaryMoodsJson")) ? "[]" : reader.GetString(reader.GetOrdinal("SecondaryMoodsJson"));
                var moods = JsonSerializer.Deserialize<List<string>>(secondaryJson, _jsonOptions) ?? new List<string>();
                
                var moodComment = reader.IsDBNull(reader.GetOrdinal("MoodComment")) ? string.Empty : reader.GetString(reader.GetOrdinal("MoodComment"));
                
                var tagsJson = reader.IsDBNull(reader.GetOrdinal("TagsJson")) ? "[]" : reader.GetString(reader.GetOrdinal("TagsJson"));
                var tags = JsonSerializer.Deserialize<List<string>>(tagsJson, _jsonOptions) ?? new List<string>();
                
                var createdStr = reader.IsDBNull(reader.GetOrdinal("CreatedAt")) ? DateTime.UtcNow.ToString("o") : reader.GetString(reader.GetOrdinal("CreatedAt"));
                var created = DateTime.Parse(createdStr);
                
                var updatedStr = reader.IsDBNull(reader.GetOrdinal("UpdatedAt")) ? DateTime.UtcNow.ToString("o") : reader.GetString(reader.GetOrdinal("UpdatedAt"));
                var updated = DateTime.Parse(updatedStr);

                var isLocked = reader.IsDBNull(reader.GetOrdinal("IsLocked")) ? 0 : reader.GetInt32(reader.GetOrdinal("IsLocked"));
                
                DateTime? lockedAt = null;
                if (!reader.IsDBNull(reader.GetOrdinal("LockedAt")))
                {
                    lockedAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("LockedAt")));
                }

                var optionalNote = reader.IsDBNull(reader.GetOrdinal("OptionalNote")) ? string.Empty : reader.GetString(reader.GetOrdinal("OptionalNote"));

                return new JournalEntry
                {
                    Id = id,
                    Date = date,
                    Title = title,
                    Content = content,
                    PrimaryMood = primary,
                    SecondaryMoods = moods,
                    MoodComment = moodComment,
                    Tags = tags,
                    CreatedAt = created,
                    UpdatedAt = updated,
                    IsLocked = isLocked == 1,
                    LockedAt = lockedAt,
                    OptionalNote = optionalNote
                };
            }
            catch
            {
                return new JournalEntry { Id = Guid.NewGuid(), Date = DateTime.UtcNow, Title = "Error Reading Entry" };
            }
        }

        // Utility for tests / debugging
        public string GetDatabasePath() => _dbPath;
    }
}
