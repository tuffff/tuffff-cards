using nietras.SeparatedValues;
using PuppeteerSharp;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Linq;
using tuffCards.Presets;
using tuffCards.Repositories;
using tuffLib.Dictionaries;

namespace tuffCards.Commands;

public class Converter {
	private readonly FolderRepository FolderRepository;
	private readonly MarkdownParserFactory MarkdownParserFactory;
	private readonly ILogger<Converter> Logger;

	public Converter(FolderRepository folderRepository, MarkdownParserFactory markdownParserFactory, ILogger<Converter> logger) {
		FolderRepository = folderRepository;
		MarkdownParserFactory = markdownParserFactory;
		Logger = logger;
	}

	public async Task Convert(string target, string? type, int? batchSize, bool generateImage, bool watchFiles, bool createBacks, bool overview) {
		try {
			if (batchSize is < 1) {
				throw new Exception("Batch size cannot be < 1");
			}
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

				var cardDataFilename = $"{templateName}.csv";
				var cardDataPath = Path.Combine(templatePath.Directory!.FullName, cardDataFilename);
				if (!File.Exists(cardDataPath)) {
					Logger.LogWarning("Card data file {cardDataPath} missing, skipping.", templatePath);
					continue;
				}

				Template cardTemplate;
				try {
					cardTemplate = GetCardTemplate(templatePath);
				}
				catch (Exception ex) {
					Logger.LogWarning("Error parsing card type template. Skipping. Message: {message}", ex.Message);
					continue;
				}

				var decks = await RenderCards(cardTemplate, templateName, cardDataPath, parser);
				var cardTypeCss = GetCardTypeCss(templatePath);

				var model = new WrapperModel {
					name = templateName,
					cardtypecss = cardTypeCss,
					globaltargetcss = globalTargetCss,
					scripts = scripts
				};

				decksNames.AddRange(await RenderDecks(decks, model, batchSize, generateImage, createBacks, parser, targetTemplate, outputDirectory, cardTemplate));
			}

			if (overview)
				await RenderOverview(decksNames, target);

			Logger.LogSuccess("Finished.");

			if (watchFiles)
				WatchFiles(target, type, batchSize, generateImage, createBacks, overview);
		}
		catch (Exception ex) {
			Logger.LogError("While converting: {Message}", ex.Message);
		}
	}

	private async Task RenderOverview(IEnumerable<string> renderedDecks, string target) {
		var outputPath = Path.Combine(FolderRepository.GetOutputRootDirectory(), $"{target}.html");
		var template = Template.Parse(Defaults.Overview);
		var model = new ScriptObject {
			["decks"] = renderedDecks,
			["target"] = target
		};
		var outputResult = await template.RenderAsync(model);
		await WriteTarget(outputPath, outputResult);
		Logger.LogInformation("Created overview for {count} decks", renderedDecks.Count());
	}

	private async Task<IEnumerable<string>> RenderDecks(List<(string deckName, List<(string title, string content)> cards)> decks, WrapperModel model, int? batchSize, bool generateImage, bool createBacks, CustomMarkdownParser parser, Template targetTemplate, string outputDirectory, Template cardTemplate) {
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
						var outputResult = await RenderWithModel(model, parser, targetTemplate);
						var batchName = batchType switch {
							BatchType.All => $"{deck.deckName}",
							BatchType.Single => $"{deck.deckName} {FolderRepository.MakeValidFileName(batch.First().title)}",
							_ => $"{deck.deckName} {batchIndex}"
						};
						var outputPath = await CreateTarget(generateImage, outputDirectory, batchName, outputResult);
						if (batchType != BatchType.All)
							Logger.LogDebug("Created {count} card(s): {outputPath}", batch.Length, outputPath);
						return batchName;
					}));
				if (createBacks) {
					await CreateBacks(generateImage, parser, cardTemplate, model, targetTemplate, outputDirectory, deck.deckName);
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
			return Enumerable.Empty<string>();
		}
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

	private async Task<List<(string deckName, List<(string title, string content)> cards)>>
		RenderCards(Template template, string templateName, string cardData, CustomMarkdownParser parser) {
		var usedNames = new Dictionary<string, int>();
		var decks = new DefaultValueDictionary<string, List<(string title, string content)>>(
			() => new List<(string title, string content)>());
		using var reader = Sep.Reader(o => o with { Unescape = true}).FromFile(cardData);
		try {
			foreach (var (data, title) in reader.Enumerate(row => {
					if (row.Span.Length == 0 || row.Span.StartsWith("//"))
						return (new(), "");
					var title = row[0].ToString();
					if (!usedNames.TryAdd(title, 1)) {
						usedNames[title] += 1;
						title = $"{title}_{usedNames[title]}";
					}
					var dict = new Dictionary<string, string>();
					foreach (var header in reader.Header.ColNames)
						dict[header] = parser.Parse(row[header].ToString());
					return (dict, title);
				}).Where(d => d.dict.Count != 0)) {
				var scriptObject = new ScriptObject();
				scriptObject.Import(data);
				scriptObject.Import("md", new Func<string, string>(parser.Parse));
				var result = await template.RenderAsync(scriptObject);
				var copies = data.TryGetValue("Copies", out var s) && int.TryParse(s, out var c) ? c : 1;
				var deckName = data.GetValueOrDefault("Deck") ?? templateName;
				Logger.LogDebug("Adding card {cardName} to deck {deckName}", title, deckName);
				decks[deckName].AddRange(Enumerable.Range(0, copies).Select(_ => (title, result)));
			}
		}
		catch (Exception ex) {
			Logger.LogError("Parsing card data: {message}. Skipping.", ex.Message);
		}
		return decks.Select(kv => (kv.Key, kv.Value)).ToList();
	}

	private async Task<string> CreateTarget(bool generateImage, string outputDirectory, string deckName, string outputResult) {
		var outputPath = Path.Combine(outputDirectory, $"{deckName}.html");
		await WriteTarget(outputPath, outputResult);
		if (generateImage)
			await GenerateImage(outputPath, outputDirectory, deckName);
		return outputPath;
	}

	private static async Task<string> RenderWithModel(WrapperModel model, CustomMarkdownParser parser, Template targetTemplate) {
		var scriptObject = new ScriptObject();
		scriptObject.Import(model);
		scriptObject.Import("md", new Func<string, string>(parser.Parse));
		var outputResult = await targetTemplate.RenderAsync(scriptObject);
		return outputResult;
	}

	private static async Task WriteTarget(string outputPath, string outputResult) {
		await using var output = new StreamWriter(outputPath, false);
		await output.WriteLineAsync(outputResult);
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

	private async Task GenerateImage(string outputPath, string outputDirectory, string name) {
		Logger.LogDebug("Generating image ... ");
		try {
			var imagePath = Path.Combine(outputDirectory, $"{name}.png");
			var browserFetcher = new BrowserFetcher(SupportedBrowser.Chrome);
			if (browserFetcher.GetInstalledBrowsers().All(c => c.Browser != SupportedBrowser.Chrome)) {
				Logger.LogInformation("Getting browser, this make take some time ...");
				await browserFetcher.DownloadAsync();
			}
			Logger.LogDebug("Launching ...");
			await using var browser = await Puppeteer.LaunchAsync(new LaunchOptions {
				Headless = true,
				Browser = SupportedBrowser.Chrome,
				DefaultViewport = new ViewPortOptions { Width = 1, Height = 1 }
			});
			Logger.LogDebug("Navigating ...");
			await using var page = await browser.NewPageAsync();
			await page.GoToAsync(outputPath);
			Logger.LogDebug("Screenshotting ...");
			await page.ScreenshotAsync(imagePath, new ScreenshotOptions { FullPage = true });
			Logger.LogInformation("Added image: {imagePath}", imagePath);
		}
		catch (Exception ex) {
			Logger.LogError("Generating image: {message}. Skipping.", ex.Message);
		}
	}

	private void WatchFiles(string target, string? type, int? batchSize, bool image, bool createBacks, bool overview) {
		var cardsWatcher = type != null
			? new FileSystemWatcher { Filters = { $"{target}.csv", $"{target}.html", $"{target}.css" } }
			: new FileSystemWatcher { Filters = { "*.csv", "*.html", "*.css" } };
		cardsWatcher.Path = FolderRepository.GetCardsDirectory();
		cardsWatcher.EnableRaisingEvents = true;
		var cardsEvents = Observable
			.FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(
				h => cardsWatcher.Changed += h,
				h => cardsWatcher.Changed -= h)
			.Select(e => e.EventArgs);

		var targetWatcher = new FileSystemWatcher {
			Path = FolderRepository.GetTargetDirectory(),
			EnableRaisingEvents = true,
			Filters = { $"{target}.html", "global.css" }
		};
		var targetEvents = Observable
			.FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(
				h => targetWatcher.Changed += h,
				h => targetWatcher.Changed -= h)
			.Select(e => e.EventArgs);

		var observable = cardsEvents.Merge(targetEvents);
		observable.Throttle(TimeSpan.FromMilliseconds(200)).Subscribe(arg => {
			Logger.LogDebug("File changed: {path}", arg.FullPath);
			Convert(target, type, batchSize, image, false, createBacks, overview).Wait();
		Logger.LogSuccess("Still watching. Press q to quit.");
		});
		Logger.LogSuccess("Watching files. Press q to quit.");
		while (Console.ReadKey().KeyChar != 'q') {}
	}

	private async Task CreateBacks(bool generateImage, CustomMarkdownParser parser, Template cardTemplate, WrapperModel model, Template targetTemplate, string outputDirectory, string name) {
		var backContent = new ScriptObject();
		backContent.Import("md", new Func<string, string>(parser.Parse));
		backContent["Deck"] = name;
		var backCardResult = await cardTemplate.RenderAsync(backContent);
		model.cards = new List<string> { backCardResult };
		var backOutputResult = await RenderWithModel(model, parser, targetTemplate);
		await CreateTarget(generateImage, outputDirectory, $"{name} back", backOutputResult);
	}

	[SuppressMessage("ReSharper", "InconsistentNaming")]
	[SuppressMessage("ReSharper", "IdentifierTypo")]
	[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
	private class WrapperModel {
		public string name { get; set; } = "";
		public IList<string> cards { get; set; } = new List<string>();
		public string cardtypecss { get; set; } = "";
		public string globaltargetcss { get; set; } = "";
		public IList<string> scripts { get; set; } = new List<string>();
	}

	private enum BatchType {
		All,
		Single,
		Batch
	}
}