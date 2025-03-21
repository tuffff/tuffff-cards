﻿namespace tuffCards;

public static class Program {
	public static async Task Main(string[] args) {
		var targetArg = new Option<string>("--target", () => "default", "The name of the target file (in /targets).");
		var cardTypeArg = new Option<string?>("--type", () => null, "Only creates card types where the name contains this argument.");
		var batchSizeArg = new Option<int?>("--batch-size", () => null, "Breaks output into multiple parts when they contain more cards than the size.");
		var singleArg = new Option<bool>("--single", () => false, "Shortcut for \"--batch-size 1\"");
		var generateImageArg = new Option<bool>("--image", () => false, "Creates a png by taking a render after generating the html (with chrome expected at default path). You should specify the size in the target.");
		var bleedArg = new Option<string?>("--bleed", () => null, "Adds a simple bleed for printing to the cards' image by extending the outermost pixels. Accepted units: px, mm, cm, in, pt, pc.");
		var createBacksArg = new Option<bool>("--backs", () => false, "Create a card back by filling the template with no data.");
		var overviewArg = new Option<bool>("--overview", () => false, "Create an overview file with the target name in the output folder.");
		var watchFilesArg = new Option<bool>("--watch", () => false, "Watches all used input files and starts another completion on changes.");
		var forceArg = new Option<bool>("--force", () => false, "Force the command, ignore warnings.");
		var openFilesArg = new Option<bool>("--open-files", () => false, "Automatically open the generated files or the overview file.");
		var logLevelArg = new Option<LogLevel>("--log-level", () => LogLevel.Information, "Sets the log level.");
		var cardTypeNameArg = new Argument<string>("name", "The name of the card type (also the file name).");

		var root = new RootCommand("tuffCards is a small tool to convert html/css/csv templates to cards.");

		var convertCmd = new Command("convert", "Converts the cards to html pages. Special column names are: \"Deck\" and \"Copies\".") {
			targetArg,
			cardTypeArg,
			batchSizeArg,
			singleArg,
			generateImageArg,
			bleedArg,
			watchFilesArg,
			createBacksArg,
			overviewArg,
			openFilesArg,
			logLevelArg
		};
		root.AddCommand(convertCmd);
		convertCmd.SetHandler(async context => {
			var target = context.ParseResult.GetValueForOption(targetArg)!;
			var cardType = context.ParseResult.GetValueForOption(cardTypeArg);
			var batchSize = context.ParseResult.GetValueForOption(singleArg) ? 1 : context.ParseResult.GetValueForOption(batchSizeArg);
			var generateImage = context.ParseResult.GetValueForOption(generateImageArg);
			var bleed = context.ParseResult.GetValueForOption(bleedArg);
			var watchFiles = context.ParseResult.GetValueForOption(watchFilesArg);
			var createBacks = context.ParseResult.GetValueForOption(createBacksArg);
			var overview = context.ParseResult.GetValueForOption(overviewArg);
			var openFiles = context.ParseResult.GetValueForOption(openFilesArg);
			var logLevel = context.ParseResult.GetValueForOption(logLevelArg);
			await GetServices(logLevel).GetRequiredService<Converter>().Convert(target, cardType, batchSize, bleed, generateImage, watchFiles, createBacks, overview, openFiles);
		});

		var createCmd = new Command("create", "Creates a new, relatively empty project in this folder.") {
			forceArg,
			logLevelArg
		};
		root.AddCommand(createCmd);
		createCmd.SetHandler(async (force, logLevel) => await GetServices(logLevel).GetRequiredService<Creator>().Create(force),
			forceArg, logLevelArg);

		var createExampleCmd = new Command("create-example", "Creates a new project with example cards in this folder.") {
			forceArg,
			logLevelArg
		};
		root.AddCommand(createExampleCmd);
		createExampleCmd.SetHandler(async (force, logLevel) => await GetServices(logLevel).GetRequiredService<Creator>().CreateExample(force),
			forceArg, logLevelArg);

		var addCardType = new Command("add-type", "Adds a new card type to the project. A .html, .csv and .css file is created.") {
			cardTypeNameArg,
			forceArg,
			logLevelArg
		};
		root.AddCommand(addCardType);
		addCardType.SetHandler(async (name, force, logLevel) => await GetServices(logLevel).GetRequiredService<CardTypeAdder>().Add(name, force),
			cardTypeNameArg, forceArg, logLevelArg);

		await root.InvokeAsync(args);
	}

	private static ServiceProvider GetServices(LogLevel logLevel) {
		return new ServiceCollection()
			.AddLogging(b => b
				.AddConsole(options => options.FormatterName=nameof(CustomConsoleFormatter))
				.AddConsoleFormatter<CustomConsoleFormatter, CustomConsoleFormatterOptions>()
				.SetMinimumLevel(logLevel))
			.AddScoped<CardTypeAdder>()
			.AddScoped<Converter>()
			.AddScoped<Creator>()
			.AddScoped<MarkdownParserFactory>()
			.AddScoped<CsvReader>()
			.AddScoped<OdsReader>()
			.AddScoped<XlsxReader>()
			.AddSingleton<BrowserService>()
			.AddScoped(_ => new FolderRepository(Directory.GetCurrentDirectory()))
			.BuildServiceProvider();
	}
}