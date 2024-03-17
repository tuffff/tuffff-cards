using PuppeteerSharp;
using System.Diagnostics.CodeAnalysis;
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

	public async Task Convert(string target, bool image) {
		try {
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

				var cardDataFilename = $"{name}.csv";
				var cardDataPath = Path.Combine(cardTemplatePath.Directory!.FullName, cardDataFilename);
				if (!File.Exists(cardDataPath)) {
					Logger.LogWarning("Card data file {cardDataPath} missing, skipping.", cardTemplatePath);
					continue;
				}

				Template cardTemplate;
				try {
					cardTemplate = GetCardTemplate(cardTemplatePath, targetTemplate);
				}
				catch (Exception ex) {
					Logger.LogWarning("Error parsing card type template. Skipping. Message: {message}", ex.Message);
					continue;
				}

				var cards = await GetCardData(cardDataPath, parser, cardTemplate);
				var cardTypeCss = GetCardTypeCss(cardTemplatePath);

				var model = new WrapperModel {
					name = name,
					cards = cards,
					cardtypecss = cardTypeCss,
					globaltargetcss = globalTargetCss,
					scripts = scripts
				};
				var outputResult = await CreateCards(model, parser, targetTemplate);
				var outputPath = Path.Combine(outputDirectory, $"{name}.html");
				try {
					await WriteTarget(outputPath, cards, outputResult);
				}
				catch (Exception ex) {
					Logger.LogError("Writing output: {message}. Skipping.", ex.Message);
					continue;
				}

				if (image) {
					await GenerateImage(outputPath, outputDirectory, name);
				}
			}
			Logger.LogSuccess("Finished.");
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

	private static Template GetCardTemplate(FileSystemInfo cardTemplatePath, Template targetTemplate) {
		var templateText = File.ReadAllText(cardTemplatePath.FullName);
		var template = Template.Parse(templateText);
		if (targetTemplate.HasErrors) throw new InvalidOperationException(targetTemplate.Messages.ToString());
		return template;
	}

	private async Task<List<string>> GetCardData(string cardData, TuffCardsMarkdownParser parser, Template template) {
		var cards = new List<string>();
		using var reader = new StreamReader(cardData);
		try {
			var headers = (await reader.ReadLineAsync())?.Split(';');
			if (headers == null) throw new Exception("Empty header line");
			while (await reader.ReadLineAsync() is {} line) {
				var data = headers
					.Zip(line.Split(';'), (header, row) => new { header, row })
					.ToDictionary(x => x.header, x => parser.Parse(x.row));
				var scriptObject = new ScriptObject();
				scriptObject.Import(data);
				scriptObject.Import("md", new Func<string, string>(parser.Parse));
				var result = await template.RenderAsync(scriptObject);
				cards.Add(result);
			}
		}
		catch (Exception ex) {
			Logger.LogError("Parsing card data: {message}. Skipping.", ex.Message);
		}
		return cards;
	}

	private static async Task<string> CreateCards(WrapperModel model, TuffCardsMarkdownParser parser, Template targetTemplate) {
		var scriptObject = new ScriptObject();
		scriptObject.Import(model);
		scriptObject.Import("md", new Func<string, string>(parser.Parse));
		var outputResult = await targetTemplate.RenderAsync(scriptObject);
		return outputResult;
	}

	private async Task WriteTarget(string outputPath, IReadOnlyCollection<string> cards, string outputResult) {
		await using var output = new StreamWriter(outputPath, false);
		Logger.LogInformation("Created {cardsCount} cards: {outputPath}", cards.Count, outputPath);
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
			var browserFetcher = new BrowserFetcher(SupportedBrowser.Firefox);
			if (!browserFetcher.GetInstalledBrowsers().Any(b => b.Browser == SupportedBrowser.ChromeHeadlessShell)) {
				Logger.LogInformation("Getting browser, this make take some time ...");
			}
			await browserFetcher.DownloadAsync();
			Logger.LogDebug("Launching ...");
			var browser = await Puppeteer.LaunchAsync(new LaunchOptions { Headless = true });
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
}