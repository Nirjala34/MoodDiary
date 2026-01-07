using Microsoft.Extensions.Logging;
using JournalApp.Services;
using Microsoft.EntityFrameworkCore;
namespace JournalApp; 
using JournalApp.Data;
public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
			});

		builder.Services.AddMauiBlazorWebView();
		// Use SQLite-backed storage for journal entries
		builder.Services.AddDbContext<JournalDbContext>(options =>
{
    options.UseSqlite("Data Source=journal.db");
});
		builder.Services.AddSingleton<IJournalStorageService, SqliteJournalStorageService>();
		builder.Services.AddScoped<AnalyticsService>();
		builder.Services.AddScoped<StreakService>();
		builder.Services.AddSingleton<UserService>();
		builder.Services.AddSingleton<SecurityService>();	
		builder.Services.AddSingleton<ThemeService>();


#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
