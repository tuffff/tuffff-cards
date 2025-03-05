using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using tuffCards.Commands;
using tuffCards.Markdown;
using tuffCards.Services;
using tuffCards.Tests.Helpers;
using Xunit.DependencyInjection.Logging;

namespace tuffCards.Tests;

public static class Startup {
	public static void ConfigureServices(IServiceCollection services) {
		services
			.AddLogging(b => b
				.AddXunitOutput()
				.SetMinimumLevel(LogLevel.Trace))
			.AddScoped<ILoggerFactory, LoggerFactory>()
			.AddScoped<CardTypeAdder>()
			.AddScoped<Converter>()
			.AddScoped<Creator>()
			.AddScoped<MarkdownParserFactory>()
			.AddScoped(_ => new FolderRepository(Directory.GetCurrentDirectory()))
			.AddScoped<TemporaryDirectoryFactory>();
	}
}