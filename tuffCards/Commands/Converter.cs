using PuppeteerSharp;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Linq;
using tuffCards.Repositories;

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

	public async Task Convert(string target, string? type, int? batchSize, bool generateImage, bool watchFiles, bool createBacks) {
		try {
			if (batchSize.HasValue && batchSize < 1) {
				throw new Exception("Batch size cannot be < 1");
			}
			var targetTemplate = GetTargetTemplate(target);
			var outputDirectory = FolderRepository.GetOutputDirectory(target);
			FolderRepository.ClearOutputDirectory(target);
			var parser = MarkdownParserFactory.Build(target);
			var globalTargetCss = GetGlobalTargetCss();
			var scripts = GetScripts();

			foreach (var cardTemplatePath in GetCardTemplatePaths()) {
				var name = Path.GetFileNameWithoutExtension(cardTemplatePath.Name);
				using var state = Logger.BeginScope(name);
				Logger.LogDebug("Card type: {name}", name);
				if (type != null && !name.Contains(type)) {
					Logger.LogDebug("Skipping type, does not contain {type}", type);
					continue;
				}

				var cardDataFilename = $"{name}.csv";
				var cardDataPath = Path.Combine(cardTemplatePath.Directory!.FullName, cardDataFilename);
				if (!File.Exists(cardDataPath)) {
					Logger.LogWarning("Card data file {cardDataPath} missing, skipping.", cardTemplatePath);
					continue;
				}

				Template cardTemplate;
				try {
					cardTemplate = GetCardTemplate(cardTemplatePath);
				}
				catch (Exception ex) {
					Logger.LogWarning("Error parsing card type template. Skipping. Message: {message}", ex.Message);
					continue;
				}

				var cards = await RenderCard(cardDataPath, parser, cardTemplate);
				var cardTypeCss = GetCardTypeCss(cardTemplatePath);

				var model = new WrapperModel {
					name = name,
					cardtypecss = cardTypeCss,
					globaltargetcss = globalTargetCss,
					scripts = scripts
				};

				try {
					var batchType = batchSize switch {
						1 => BatchType.Single,
						{} x when x < cards.Count => BatchType.Batch,
						_ => BatchType.All
					};
					foreach (var (batch, batchIndex) in cards.Chunk(batchSize ?? cards.Count).Select((b, i) => (b.ToList(), i))) {
						model.cards = batch.Select(c => c.result).ToList();
						var outputResult = await PrepareTargetModel(model, parser, targetTemplate);
						var cardName = batchType switch {
							BatchType.All => $"{name}",
							BatchType.Single => $"{name} {FolderRepository.MakeValidFileName(batch.First().firstColumn)}",
							_ => $"{name} {batchIndex}"
						};
						var outputPath = await CreateTarget(generateImage, outputDirectory, cardName, outputResult);
						if (batchType != BatchType.All)
							Logger.LogDebug("Created {count} card(s): {outputPath}", batch.Count, outputPath);
					}
					if (createBacks) {
						await CreateBacks(generateImage, parser, cardTemplate, model, targetTemplate, outputDirectory, name);
					}
					Logger.LogInformation("Created {count} cards in {outputPath}", cards.Count, outputDirectory);
				}
				catch (Exception ex) {
					Logger.LogError("Writing output: {message}. Skipping.", ex.Message);
				}
			}
			Logger.LogSuccess("Finished.");
			if (watchFiles) {
				WatchFiles(target, type, batchSize, generateImage, createBacks);
			}
		}
		catch (Exception ex) {
			Logger.LogError("While converting: {Message}", ex.Message);
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

	private async Task<List<(string firstColumn, string result)>> RenderCard(string cardData, TuffCardsMarkdownParser parser, Template template) {
		var cards = new List<(string, string)>();
		var usedNames = new Dictionary<string, int>();
		using var reader = new StreamReader(cardData);
		try {
			var headers = (await reader.ReadLineAsync())?.Split(';');
			if (headers == null) throw new Exception("Empty header line");
			while (await reader.ReadLineAsync() is {} line) {
				var cells = line.Split(';');
				var title = cells.FirstOrDefault() ?? "card";
				if (!usedNames.TryAdd(title, 1)) {
					usedNames[title] += 1;
					title = $"{title}_{usedNames[title]}";
				}

                var data = headers
					.Zip(cells, (header, row) => new { header, row })
					.ToDictionary(x => x.header, x => parser.Parse(x.row));
				var scriptObject = new ScriptObject();
				scriptObject.Import(data);
				scriptObject.Import("md", new Func<string, string>(parser.Parse));
				var result = await template.RenderAsync(scriptObject);
				cards.Add((title, result));
			}
		}
		catch (Exception ex) {
			Logger.LogError("Parsing card data: {message}. Skipping.", ex.Message);
		}
		return cards;
	}

	private async Task<string> CreateTarget(bool generateImage, string outputDirectory, string cardName, string outputResult) {
		var outputPath = Path.Combine(outputDirectory, $"{cardName}.html");
		await WriteTarget(outputPath, outputResult);
		if (generateImage)
			await GenerateImage(outputPath, outputDirectory, cardName);
		return outputPath;
	}

	private static async Task<string> PrepareTargetModel(WrapperModel model, TuffCardsMarkdownParser parser, Template targetTemplate) {
		var scriptObject = new ScriptObject();
		scriptObject.Import(model);
		scriptObject.Import("md", new Func<string, string>(parser.Parse));
		var outputResult = await targetTemplate.RenderAsync(scriptObject);
		return outputResult;
	}

	private async Task WriteTarget(string outputPath, string outputResult) {
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
			if (!browserFetcher.GetInstalledBrowsers().Any(c => c.Browser == SupportedBrowser.Chrome)) {
				Logger.LogInformation("Getting browser, this make take some time ...");
				await browserFetcher.DownloadAsync();
			}
			Logger.LogDebug("Launching ...");
			var browser = await Puppeteer.LaunchAsync(new LaunchOptions {
				Headless = true,
				Browser = SupportedBrowser.Chrome,
				DefaultViewport = new ViewPortOptions { Width = 1, Height = 1 }
			});
			Logger.LogDebug("Navigating ...");
			var page = await browser.NewPageAsync();
			await page.GoToAsync(outputPath);
			Logger.LogDebug("Screenshotting ...");
			await page.ScreenshotAsync(imagePath, new ScreenshotOptions { FullPage = true });
			Logger.LogInformation("Added image: {imagePath}", imagePath);
		}
		catch (Exception ex) {
			Logger.LogError("Generating image: {message}. Skipping.", ex.Message);
		}
	}

	private void WatchFiles(string target, string? type, int? batchSize, bool image, bool createBacks) {
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
			Convert(target, type, batchSize, image, false, createBacks).Wait();
		Logger.LogSuccess("Still watching. Press q to quit.");
		});
		Logger.LogSuccess("Watching files. Press q to quit.");
		while (Console.ReadKey().KeyChar != 'q') {}
	}

	private async Task CreateBacks(bool generateImage, TuffCardsMarkdownParser parser, Template cardTemplate, WrapperModel model, Template targetTemplate, string outputDirectory, string name) {
		var backContent = new ScriptObject();
		backContent.Import("md", new Func<string, string>(parser.Parse));
		var backCardResult = await cardTemplate.RenderAsync(backContent);
		model.cards = new List<string> { backCardResult };
		var backOutputResult = await PrepareTargetModel(model, parser, targetTemplate);
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