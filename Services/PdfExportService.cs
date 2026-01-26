using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using JournalApp.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;

namespace JournalApp.Services
{
    public class PdfExportService
    {
        private readonly IJournalStorageService _storage;

        public PdfExportService(IJournalStorageService storage)
        {
            _storage = storage;
            QuestPDF.Settings.License = LicenseType.Community;
        }

        //  OLD METHOD — date-range based
        public async Task<byte[]> ExportJournalEntriesAsync(DateTime fromDate, DateTime toDate)
        {
            var entries = (await _storage.GetEntriesAsync())
                .Where(e => e.CreatedAt.Date >= fromDate.Date &&
                            e.CreatedAt.Date <= toDate.Date)
                .OrderBy(e => e.CreatedAt)
                .ToList();

            return GeneratePdf(entries, fromDate, toDate);
        }

        //  NEW METHOD — accept filtered entries directly
        public async Task<byte[]> ExportJournalEntriesAsync(List<JournalEntry> filteredEntries)
        {
            if (filteredEntries == null || !filteredEntries.Any())
                throw new Exception("No entries to export.");

            // Use the earliest and latest dates from filtered entries for header
            var fromDate = filteredEntries.Min(e => e.CreatedAt);
            var toDate = filteredEntries.Max(e => e.CreatedAt);

            return GeneratePdf(filteredEntries, fromDate, toDate);
        }
        
        

        //  PDF generation helper
        private byte[] GeneratePdf(List<JournalEntry> entries, DateTime fromDate, DateTime toDate)
        {
            using var stream = new MemoryStream();

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);
                    page.Size(QuestPDF.Helpers.PageSizes.A4);
                    page.PageColor(QuestPDF.Helpers.Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(12));

                    // Header
                    page.Header()
                        .Text($"Journal Export: {fromDate:dd MMM yyyy} - {toDate:dd MMM yyyy}")
                        .FontSize(16)
                        .Bold();

                    // Content
                    page.Content().Column(col =>
                    {
                        foreach (var entry in entries)
                        {
                            col.Item().PaddingBottom(10).Text(text =>
                            {
                                text.Line($"📝 Title: {entry.Title}");
                                text.Line($"📅 Date: {entry.CreatedAt:dd MMM yyyy}");
                                text.Line($"🎭 Primary Mood: {entry.PrimaryMood}");

                                if (entry.SecondaryMoods?.Any() == true)
                                    text.Line($"🌈 Secondary Moods: {string.Join(", ", entry.SecondaryMoods)}");

                                if (entry.Tags?.Any() == true)
                                    text.Line($"🏷 Tags: {string.Join(", ", entry.Tags)}");

                                if (!string.IsNullOrWhiteSpace(entry.OptionalNote))
                                    text.Line($"🗒 Optional Note: {entry.OptionalNote}");

                                text.Line($"🖋 Content: {entry.Content}");
                                text.Line("----------------------------------------");
                            });
                        }
                    });

                    // Footer
                    page.Footer()
                        .AlignCenter()
                        .Text("Generated with JournalApp");
                });
            })
            .GeneratePdf(stream);

            return stream.ToArray();
        }
    }
}
