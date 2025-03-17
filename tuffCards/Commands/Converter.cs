using PuppeteerSharp;
using System.Diagnostics;
using System.Reactive.Linq;
using tuffCards.Commands.ConverterModels;
using tuffCards.Presets;
using tuffCards.Services;
using tuffLib.Dictionaries;

namespace tuffCards.Commands;

public class Converter(
	FolderRepository FolderRepository,
	MarkdownParserFactory MarkdownParserFactory,
	ILogger<Converter> Logger,
	BrowserService BrowserService,
	CsvReader CsvReader,
	OdsReader OdsReader,
	XlsxReader XlsxReader) {

	public async Task Convert(string target, string? type, int? batchSize, string? bleed, bool generateImage, bool watchFiles, bool createBacks, bool overview, bool openFiles) {
		try {
			if (batchSize is < 1) {
				throw new Exception("Batch size cannot be < 1");
			}
			var bleedPixels = bleed.ToPixels();
			Logger.LogDebug("Bleed value: {bleedValue}", bleedPixels);
			var targetTemplate = GetTargetTemplate(target);
			var outputDirectory = FolderRepository.GetOutputDirectory(target);
			FolderRepository.ClearOutputDirectory(target);
			var parser = MarkdownParserFactory.Build(target);
			var globalTargetCss = GetGlobalTargetCss();
			var scripts = GetScripts();
			var templatePaths = GetCardTemplatePaths();
			var decksNames = new List<string>();

			foreach (var templatePath in templatePaths) {
				var templateName = Path.GetFileNameWithoutExtension(templatePath.Name);
				using var state = Logger.BeginScope(templateName);
				Logger.LogDebug("Card type: {name}", templateName);

				if (type != null && !templateName.Contains(type)) {
					Logger.LogDebug("Skipping type, does not contain {type}", type);
					continue;
				}

				var cardInfos = FindData(templatePath.Directory!.FullName, templateName);
				if (cardInfos == null) {
					Logger.LogWarning("Card data file {cardDataPath} missing, skipping.", templatePath);
					continue;
				}
				var (cardData, cardDataPath) = cardInfos.Value;
				Logger.LogDebug("Using card data: {path}", cardDataPath);

				Template cardTemplate;
				try {
					cardTemplate = GetCardTemplate(templatePath);
				}
				catch (Exception ex) {
					Logger.LogWarning("Error parsing card type template. Skipping. Message: {message}", ex.Message);
					continue;
				}

				var decks = await RenderCards(cardTemplate, templateName, cardData, parser);
				var cardTypeCss = GetCardTypeCss(templatePath);

				var model = new DeckModel(parser) {
					name = templateName,
					cardtypecss = cardTypeCss,
					globaltargetcss = globalTargetCss,
					scripts = scripts,
				};

				decksNames.AddRange(
					await RenderDecks(decks, model, batchSize, generateImage, bleedPixels, createBacks, parser, targetTemplate, outputDirectory, cardTemplate, overview, openFiles)
				);
			}

			if (overview)
				await RenderOverview(decksNames, target, openFiles);

			Logger.LogSuccess("Finished.");

			if (watchFiles)
				WatchFiles(target, type, batchSize, generateImage, bleedPixels, createBacks, overview);
		}
		catch (Exception ex) {
			Logger.LogError("While converting: {Message}", ex.Message);
		}
	}

	private (List<(string name, Dictionary<string, string> data)> data, string path)? FindData(string templatePath, string templateName) {
		foreach (var option in new List<(string filename, Func<string, List<(string name, Dictionary<string, string> data)>> handler)> {
				($"{templateName}.csv", CsvReader.GetData),
				($"{templateName}.ods", path => GetAllData(OdsReader.GetDocument(path))),
				($"{templateName}.xlsx", path => GetAllData(XlsxReader.GetDocument(path))),
				("data.ods", path => GetSheetData(OdsReader.GetDocument(path))),
				("data.xlsx", path => GetSheetData(XlsxReader.GetDocument(path)))
			}) {
			var cardDataPath = Path.Combine(templatePath, option.filename);
			if (File.Exists(cardDataPath)) {
				return (option.handler(cardDataPath), cardDataPath);
			}
		}
		return null;

		List<(string name, Dictionary<string, string> data)> GetSheetData(ITableDocument document) {
			return document.GetSheet(templateName)?.GetContentAsDictionary().ToList() ?? [];
		}

		List<(string name, Dictionary<string, string> data)> GetAllData(ITableDocument document) {
			return document.GetAllSheets().SelectMany(s => s.GetContentAsDictionary()).ToList();
		}
	}

	private async Task RenderOverview(List<string> renderedDecks, string target, bool openFile) {
		var outputPath = Path.Combine(FolderRepository.GetOutputRootDirectory(), $"{target}.html");
		var template = Template.Parse(Defaults.Overview);
		var model = new OverviewModel {
			decks = renderedDecks,
			target = target
		};
		var outputResult = await template.RenderAsync(model);
		await WriteTarget(outputPath, outputResult, openFile);
		Logger.LogInformation("Created overview for {count} decks", renderedDecks.Count);
	}

	private async Task<IEnumerable<string>> RenderDecks(List<(string deckName, List<(string title, string content)> cards)> decks, DeckModel model, int? batchSize, bool generateImage, int bleedPixels, bool createBacks, CustomMarkdownParser parser, Template targetTemplate, string outputDirectory, Template cardTemplate, bool generateOverview, bool openFile) {
		try {
			var result = await Task.WhenAll(decks.Select(async deck => {
				var batchType = batchSize switch {
					1 => BatchType.Single,
					{} x when x < deck.cards.Count => BatchType.Batch,
					_ => BatchType.All
				};
				var batches = await Task.WhenAll(deck.cards
					.Chunk(batchSize ?? deck.cards.Count)
					.Select(async (batch, batchIndex) => {
						model.cards = batch.Select(c => c.content).ToList();
						var outputResult = await targetTemplate.RenderAsync(model);
						var batchName = batchType switch {
							BatchType.All => $"{deck.deckName}",
							BatchType.Single => $"{deck.deckName} {FolderRepository.MakeValidFileName(batch.First().title)}",
							_ => $"{deck.deckName} {batchIndex}"
						};
						var outputPath = await CreateTarget(generateImage, bleedPixels, outputDirectory, batchName, outputResult, openFile && !generateOverview);
						if (batchType != BatchType.All)
							Logger.LogDebug("Created {count} card(s): {outputPath}", batch.Length, outputPath);
						return batchName;
					}));
				if (createBacks) {
					await CreateBacks(generateImage, bleedPixels, parser, cardTemplate, model, targetTemplate, outputDirectory, deck.deckName);
				}
				return batches;
			}));
			if (decks.Count == 1)
				Logger.LogInformation("Created {count} cards", decks[0].cards.Count);
			else
				Logger.LogInformation("Created {cardCount} cards in {deckCount} decks", decks.Sum(d => d.cards.Count), decks.Count);
			return result.SelectMany(r => r);
		}
		catch (Exception ex) {
			Logger.LogError("Writing output: {message}. Skipping.", ex.Message);
			return [];
		}
	}

	private async Task<List<(string deckName, List<(string title, string content)> cards)>>
		RenderCards(Template template, string templateName, List<(string, Dictionary<string, string>)> rows, CustomMarkdownParser parser) {
		var usedNames = new Dictionary<string, int>();
		var decks = new DefaultValueDictionary<string, List<(string title, string content)>>(() => []);
		try {
			foreach (var (name, data) in rows) {
				var title = name;
				if (!usedNames.TryAdd(title, 1)) {
					usedNames[title] += 1;
					title = $"{title}_{usedNames[title]}";
				}
				var parsedData = data.ToDictionary(kv => kv.Key, kv => parser.Parse(kv.Value));
				var scriptObject = parsedData.ToScriptObject(parser);
				var result = await template.RenderAsync(scriptObject);
				var copies = parsedData.TryGetValue("Copies", out var s) && int.TryParse(s, out var c) ? c : 1;
				var deckName = parsedData.GetValueOrDefault("Deck") ?? templateName;
				Logger.LogDebug("Adding card {cardName} to deck {deckName}", title, deckName);
				decks[deckName].AddRange(Enumerable.Range(0, copies).Select(_ => (title, result)));
			}
		}
		catch (Exception ex) {
			Logger.LogError("Parsing card data: {message}. Skipping.", ex.Message);
		}
		return decks.Select(kv => (kv.Key, kv.Value)).ToList();
	}

	private async Task CreateBacks(bool generateImage, int bleedPixels, CustomMarkdownParser parser, Template cardTemplate, DeckModel model, Template targetTemplate, string outputDirectory, string name) {
		var backModel = new BackModel(parser) { deck = name };
		var backCardResult = await cardTemplate.RenderAsync(backModel);
		model.cards = new List<string> { backCardResult };
		var backOutputResult = await targetTemplate.RenderAsync(model);
		await CreateTarget(generateImage, bleedPixels, outputDirectory, $"{name} back", backOutputResult, false);
	}

	private Template GetTargetTemplate(string target) {
		try {
			var targetPath = Path.Combine(FolderRepository.GetTargetDirectory(), $"{target}.html");
			if (!File.Exists(targetPath)) throw new Exception($"Target file '{target}' not found. (path: {targetPath})");

			Logger.LogInformation("Using target template: {targetPath}", targetPath);
			var targetTemplateText = File.ReadAllText(targetPath);
			var targetTemplate = Template.Parse(targetTemplateText);
			if (targetTemplate.HasErrors) throw new InvalidOperationException(targetTemplate.Messages.ToString());
			return targetTemplate;
		}
		catch (Exception ex) {
			throw new Exception($"Error parsing target template: {ex.Message}");
		}
	}

	private IEnumerable<FileInfo> GetCardTemplatePaths() {
		return new DirectoryInfo(FolderRepository.GetCardsDirectory())
			.EnumerateFiles("*.html", SearchOption.AllDirectories);
	}

	private static Template GetCardTemplate(FileSystemInfo cardTemplatePath) {
		var templateText = File.ReadAllText(cardTemplatePath.FullName);
		var template = Template.Parse(templateText);
		if (template.HasErrors) throw new InvalidOperationException(template.Messages.ToString());
		return template;
	}

	private async Task<string> CreateTarget(bool generateImage, int bleedPixels, string outputDirectory, string deckName, string outputResult, bool openFile) {
		var outputPath = Path.Combine(outputDirectory, $"{deckName}.html");
		await WriteTarget(outputPath, outputResult, openFile);
		if (generateImage)
			await GenerateImage(outputPath, outputDirectory, deckName, bleedPixels);
		return outputPath;
	}

	private async Task WriteTarget(string outputPath, string outputResult, bool openFile) {
		await using var output = new StreamWriter(outputPath, false);
		await output.WriteLineAsync(outputResult);
		if (openFile) {
			try {
				var processStartInfo = new ProcessStartInfo {
					FileName = outputPath,
					UseShellExecute = true
				};
				Process.Start(processStartInfo);
			}
			catch (Exception ex) {
				Logger.LogError("Error opening file: {message}", ex.Message);
			}
		}
	}

	private string GetGlobalTargetCss() {
		var globalTargetCss = string.Empty;
		var globalTargetCssPath = Path.Combine(FolderRepository.GetTargetDirectory(), "global.css");
		if (File.Exists(globalTargetCssPath)) {
			try {
				globalTargetCss = File.ReadAllText(globalTargetCssPath);
			}
			catch (Exception ex) {
				Logger.LogError("Adding global target css: {message}. Skipping.", ex.Message);
			}
		}
		return globalTargetCss;
	}

	private List<string> GetScripts() {
		var scripts = new List<string>();
		foreach (var scriptFile in new DirectoryInfo(FolderRepository.GetScriptsDirectory()).EnumerateFiles("*.js")) {
			try {
				var script = File.ReadAllText(scriptFile.FullName);
				scripts.Add(script);
				Logger.LogDebug("Added script: {scriptFileName} ...", scriptFile.Name);
			}
			catch (Exception ex) {
				Logger.LogError("Adding script: {message}. Skipping.", ex.Message);
			}
		}
		return scripts;
	}

	private string GetCardTypeCss(FileInfo cardType) {
		var name = Path.GetFileNameWithoutExtension(cardType.Name);
		var cssPath = Path.Combine(cardType.Directory!.FullName, $"{name}.css");
		if (!File.Exists(cssPath)) return String.Empty;
		try {
			Logger.LogDebug("Adding css for {name} ...", name);
			return File.ReadAllText(cssPath);
		}
		catch (Exception ex) {
			Logger.LogError("Adding css: {message}. Skipping.", ex.Message);
		}
		return string.Empty;
	}

	private async Task GenerateImage(string outputPath, string outputDirectory, string name, int bleedPixels) {
		Logger.LogDebug("Generating image ... ");
		try {
			var imagePath = Path.Combine(outputDirectory, $"{name}.png");
			await using var page = await BrowserService.GetPage();
			await page.GoToAsync(outputPath);
			Logger.LogDebug("Screenshotting ...");
			await page.ScreenshotAsync(imagePath, new ScreenshotOptions { FullPage = true });
			Logger.LogDebug("... done");
			if (bleedPixels > 0) {
				Logger.LogDebug("Adding bleed ...");
				BleedService.AddBleed(imagePath, bleedPixels);
			}
			Logger.LogInformation("Added image: {imagePath}", imagePath);
		}
		catch (Exception ex) {
			Logger.LogError("Generating image: {message}. Skipping.", ex.Message);
		}
	}

	private void WatchFiles(string target, string? type, int? batchSize, bool image, int bleedPixels, bool createBacks, bool overview) {
		var cardsWatcher = new FileSystemWatcher {
			Path = FolderRepository.GetCardsDirectory(),
			EnableRaisingEvents = true
		};
		string[] extensions = ["csv", "html", "css", "ods"];
		foreach (var filter in extensions
			.Select(ext => type == null ? $"*.{ext}" : $"{target}.{ext}")) {
			cardsWatcher.Filters.Add(filter);
		}

		var targetWatcher = new FileSystemWatcher {
			Path = FolderRepository.GetTargetDirectory(),
			EnableRaisingEvents = true,
			Filters = { $"{target}.html", "global.css" }
		};

		var cardsEvents = Observable
			.FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(
				h => {
					cardsWatcher.Changed += h;
					cardsWatcher.Created += h;
					cardsWatcher.Deleted += h;
				},
				h => {
					cardsWatcher.Changed -= h;
					cardsWatcher.Created -= h;
					cardsWatcher.Deleted -= h;
				})
			.Select(e => e.EventArgs);
		var cardsRenameEvents = Observable
			.FromEventPattern<RenamedEventHandler, RenamedEventArgs>(
				h => cardsWatcher.Renamed += h,
				h => cardsWatcher.Renamed -= h)
			.Select(e => e.EventArgs);
		var targetEvents = Observable
			.FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(
				h => {
					targetWatcher.Changed += h;
					targetWatcher.Created += h;
					targetWatcher.Deleted += h;
				},
				h => {
					targetWatcher.Changed -= h;
					targetWatcher.Created -= h;
					targetWatcher.Deleted -= h;
				}
			).Select(e => e.EventArgs);
		var targetRenameEvents = Observable
			.FromEventPattern<RenamedEventHandler, RenamedEventArgs>(
				h => targetWatcher.Renamed += h,
				h => targetWatcher.Renamed -= h)
			.Select(e => e.EventArgs);

		var observable = cardsEvents.Merge(cardsRenameEvents).Merge(targetEvents).Merge(targetRenameEvents);
		observable.Throttle(TimeSpan.FromMilliseconds(200)).Subscribe(arg => {
			Logger.LogDebug("File changed: {path}", arg.FullPath);
			OdsReader.ClearCache();
			Convert(target, type, batchSize, bleedPixels == 0 ? null : $"{bleedPixels}px", image, false, createBacks, overview, false).Wait();
			Logger.LogSuccess("Still watching. Press q to quit.");
		});
		Logger.LogSuccess("Watching files. Press q to quit.");
		while (Console.ReadKey().KeyChar != 'q') {}
	}
}