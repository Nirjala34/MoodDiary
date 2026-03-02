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
        Task<bool> HasJournalForDateAsync(DateTime date);
        Task ExportAsync(string filePath);
        Task ClearAllAsync();
        
        Task<List<string>> GetCustomTagsAsync();
        Task AddCustomTagAsync(string tag);
        Task RemoveCustomTagAsync(string tag);
        Task SaveTagsAsync(List<string> tags);
        
        Task<List<string>> GetCustomMoodsAsync();
        Task AddCustomMoodAsync(string mood);
        Task RemoveCustomMoodAsync(string mood);
        Task SaveMoodsAsync(List<string> moods);
    }
}
