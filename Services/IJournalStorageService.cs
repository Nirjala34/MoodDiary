using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JournalApp.Models;

namespace JournalApp.Services
{
    public interface IJournalStorageService
    {
        Task<List<JournalEntry>> GetEntriesAsync();
        Task<JournalEntry?> GetEntryAsync(Guid id);
        Task SaveEntryAsync(JournalEntry entry);
        Task UpdateEntryAsync(JournalEntry entry);
        Task DeleteEntryAsync(Guid id);
        Task ExportAsync(string filePath);
        Task ImportAsync(string filePath);
        Task ClearAllAsync();

        // Custom tags and moods
        Task<List<string>> GetCustomTagsAsync();
        Task AddCustomTagAsync(string tag);
        Task RemoveCustomTagAsync(string tag);

        Task<List<string>> GetCustomMoodsAsync();
        Task AddCustomMoodAsync(string mood);
        Task RemoveCustomMoodAsync(string mood);
    }
}
