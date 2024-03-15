﻿using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;
using tuffCards.Commands;

namespace TuffCards;

public class Program {
	public static async Task Main(string[] args) {
		var logLevelArg = new Option<LogLevel>("--log-level", () => LogLevel.Information, "Sets the log level.");
		var targetArg = new Option<string>("--target", () => "default", "The name of the target file (in /targets).");
		var forceArg = new Option<bool>("--force", () => false, "Force the command, ignore warnings.");
		var generateImageArg = new Option<bool>("--image", () => false, "Creates a png by taking a render after generating the html (with chrome expected at default path). You should specify the size in the target.");
		var cardTypeNameArg = new Argument<string>("name", "The name of the card type (also the file name).");

		var root = new RootCommand("tuffCards is a small tool to convert html/css/csv templates to cards.");

		var convertCmd = new Command("convert", "Converts the cards to html pages.") {
			targetArg,
			generateImageArg,
			logLevelArg
		};
		root.AddCommand(convertCmd);
		convertCmd.SetHandler(async (target, image, logLevel) => await GetServices(logLevel).GetRequiredService<Converter>().Convert(target, image),
			targetArg, generateImageArg, logLevelArg);

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
				.AddConsole(options => options.FormatterName=nameof(TuffCardsConsoleFormatter))
				.AddConsoleFormatter<TuffCardsConsoleFormatter, TuffCardsConsoleFormatterOptions>()
				.SetMinimumLevel(logLevel))
			.AddScoped<CardTypeAdder>()
			.AddScoped<Converter>()
			.AddScoped<Creator>()
			.AddScoped<MarkdownParserFactory>()
			.AddScoped(_ => new FolderRepository(Directory.GetCurrentDirectory()))
			.BuildServiceProvider();
	}
}