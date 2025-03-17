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
			.AddScoped<OdsReader>()
			.AddScoped<CsvReader>()
			.AddScoped<XlsxReader>()
			.AddScoped(_ => new FolderRepository(Directory.GetCurrentDirectory()))
			.AddScoped<TemporaryDirectoryFactory>();
	}
}