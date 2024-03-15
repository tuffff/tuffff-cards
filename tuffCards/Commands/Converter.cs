using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

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
			var (targetTemplate, targetTemplateText) = GetTargetTemplate(target);
			var outputDirectory = FolderRepository.GetOutputDirectory(target);
			var parser = MarkdownParserFactory.Build(target);
			var globalTargetCss = GetGlobalTargetCss();
			var scripts = GetScripts();

			foreach (var cardTemplatePath in GetCardTemplatePaths()) {
				var name = Path.GetFileNameWithoutExtension(cardTemplatePath.Name);
				Logger.LogInformation($"Card type: {name} ...");

				var cardDataFilename = $"{name}.csv";
				var cardDataPath = Path.Combine(cardTemplatePath.Directory!.FullName, cardDataFilename);
				if (!File.Exists(cardDataPath)) {
					Logger.LogWarning($"Card data file {cardDataPath} missing, skipping.");
					continue;
				}

				Template cardTemplate;
				try {
					cardTemplate = GetCardTemplate(cardTemplatePath, targetTemplate);
				}
				catch (Exception ex) {
					Logger.LogWarning($"Error parsing card type template. Skipping. Message: {ex.Message}");
					continue;
				}

				var cards = await GetCardData(cardDataPath, parser, cardTemplate);
				var cardTypeCss = GetCardTypeCss(cardTemplatePath);

				var model = new WrapperModel(parser) {
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
					Logger.LogError($"Wring output: {ex.Message}. Skipping.");
					continue;
				}

				if (image) {
					await GenerateImage(outputPath, outputDirectory, name, targetTemplateText);
				}
			}
			Logger.LogSuccess("Finished.");
		}
		catch (Exception ex) {
			Logger.LogError($"While converting: {ex.Message}");
		}
	}

	private (Template template, string original) GetTargetTemplate(string target) {
		var targetPath = Path.Combine(FolderRepository.GetTargetDirectory(), $"{target}.html");
		if (!File.Exists(targetPath)) throw new Exception($"Target file '{target}' not found. (path: {targetPath})");

		Logger.LogInformation($"Using target template: {targetPath}");
		string targetTemplateText;
		Template targetTemplate;
		try {
			targetTemplateText = File.ReadAllText(targetPath);
			targetTemplate = Template.Parse(targetTemplateText);
			if (targetTemplate.HasErrors) throw new InvalidOperationException(targetTemplate.Messages.ToString());
		}
		catch (Exception ex) {
			throw new Exception($"Error parsing target template: {ex.Message}");
		}
		return (targetTemplate, targetTemplateText);
	}

	private IEnumerable<FileInfo> GetCardTemplatePaths() {
		return new DirectoryInfo(FolderRepository.GetCardsDirectory())
			.EnumerateFiles("*.html", SearchOption.AllDirectories);
	}

	private static Template GetCardTemplate(FileInfo cardTemplatePath, Template targetTemplate) {
		Template template;
		var templateText = File.ReadAllText(cardTemplatePath.FullName);
		template = Template.Parse(templateText);
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
				scriptObject.Import("md", new Func<string, string>(s => parser.Parse(s)));
				var result = await template.RenderAsync(scriptObject);
				cards.Add(result);
			}
		}
		catch (Exception ex) {
			Logger.LogError($"Parsing card data: {ex.Message}. Skipping.");
		}
		return cards;
	}

	private async Task<string> CreateCards(WrapperModel model, TuffCardsMarkdownParser parser, Template targetTemplate) {
		var scriptObject = new ScriptObject();
		scriptObject.Import(model);
		scriptObject.Import("md", new Func<string, string>(s => parser.Parse(s)));
		var outputResult = await targetTemplate.RenderAsync(scriptObject);
		return outputResult;
	}

	private async Task WriteTarget(string outputPath, IReadOnlyCollection<string> cards, string outputResult) {
		using var output = new StreamWriter(outputPath, false);
		Logger.LogInformation($"... created {cards.Count} cards: {outputPath}");
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
				Logger.LogError($"Adding global target css: {ex.Message}. Skipping.");
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
				Logger.LogInformation($"... Also added script: {scriptFile.Name} ...");
			}
			catch (Exception ex) {
				Logger.LogError($"Adding script: {ex.Message}. Skipping.");
			}
		}
		return scripts;
	}

	private string GetCardTypeCss(FileInfo cardType) {
		var name = Path.GetFileNameWithoutExtension(cardType.Name);
		var cssPath = Path.Combine(cardType.Directory!.FullName, $"{name}.css");
		if (!File.Exists(cssPath)) return String.Empty;
		try {
			Logger.LogInformation($"... Also adding css for {name} ...");
			return File.ReadAllText(cssPath);
		}
		catch (Exception ex) {
			Logger.LogError($"Adding css: {ex.Message}. Skipping.");
		}
		return string.Empty;
	}

	private async Task GenerateImage(string outputPath, string outputDirectory, string name, string targetTemplateText) {
		Console.Write("Generating image ... ");
		var imagePath = Path.Combine(outputDirectory, $"{name}.png");
		var imageSize = "1000,1000";
		var match = Regex.Match(targetTemplateText, @$"<!-- image-size-{name}:(\d+)x(\d+) -->");
		if (match.Success) {
			imageSize = $"{match.Groups[1].Value},{match.Groups[2].Value}";
		}
		else {
			match = Regex.Match(targetTemplateText, @$"<!-- image-size:(\d+)x(\d+) -->");
			if (match.Success) {
				imageSize = $"{match.Groups[1].Value},{match.Groups[2].Value}";
			}
		}
		const string exe = """
		                   C:\Program Files\Google\Chrome\Application\chrome.exe
		                   """;
		var args = $"""
		            --headless --screenshot="{imagePath}" --window-size="{imageSize}" "{outputPath}"
		            """;
		try {
			var pi = new ProcessStartInfo(exe, args) {
				CreateNoWindow = true,
				UseShellExecute = false,
				RedirectStandardError = true
			};
			var process = Process.Start(pi);
			process?.WaitForExit();
			if (process?.ExitCode != 0) {
				throw new Exception(await process?.StandardError.ReadToEndAsync()!);
			}

			Logger.LogInformation($"done: {imagePath}");
		}
		catch (Exception ex) {
			Logger.LogError($"Generating image: {ex.Message}. Skipping. Command was: \"{exe}\" {args}");
		}
	}

	[SuppressMessage("ReSharper", "InconsistentNaming")]
	[SuppressMessage("ReSharper", "IdentifierTypo")]
	private class WrapperModel {
		private readonly TuffCardsMarkdownParser Parser;
		public WrapperModel(TuffCardsMarkdownParser parser) {
			Parser = parser;
		}
		public string name { get; set; } = "";
		public IList<string> cards { get; set; } = new List<string>();
		public string cardtypecss { get; set; } = "";
		public string globaltargetcss { get; set; } = "";
		public IList<string> scripts { get; set; } = new List<string>();
		public string md(string s) {
			return Parser.Parse(s);
		}
	}
}