using Scriban;
using Scriban.Runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TuffCards;

public static class Converter {
	public static async Task Convert(string target, bool image) {
		try {
			var directory = new DirectoryInfo(Environment.CurrentDirectory).FullName;
			var targetDirectory = Path.Combine(".", "targets");
			var cardsDirectory = Path.Combine(".", "cards");
			var outputRootDirectory = Path.Combine(".", "output");
			var outputDirectory = Path.Combine(outputRootDirectory, target);
			var iconDirectory = Path.Combine(".", "icons");
			var imageDirectory = Path.Combine(".", "images");
			var scriptsDirectory = Path.Combine(".", "scripts");

			Log.Info($"Project directory: {directory}");

			if (!Directory.Exists(targetDirectory)) throw new Exception("Directory '/targets' not found. Did you create a new project with 'tuffCards create'?");
			if (!Directory.Exists(cardsDirectory)) throw new Exception("Directory '/cards' not found. Did you create a new project with 'tuffCards create'?");
			if (!Directory.Exists(outputRootDirectory)) Directory.CreateDirectory(outputDirectory);
			if (!Directory.Exists(outputDirectory)) Directory.CreateDirectory(outputDirectory);
			var outputDirectoryInfo = new DirectoryInfo(outputDirectory);
			foreach (var file in outputDirectoryInfo.GetFiles()) file.Delete();
			foreach (var dir in outputDirectoryInfo.GetDirectories()) dir.Delete(true);

			var targetPath = Path.Combine(targetDirectory, $"{target}.html");
			if (!File.Exists(targetPath)) throw new Exception($"Target file '{target}' not found. (path: {targetPath})");

			Log.Info($"Using target template: {targetPath}");
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

			var parser = new MarkdownParser(iconDirectory, imageDirectory, outputDirectory);
			foreach (var cardType in new DirectoryInfo(cardsDirectory).EnumerateFiles("*.html", SearchOption.AllDirectories)) {
				var name = Path.GetFileNameWithoutExtension(cardType.Name);
				Log.Info($"Card type: {name} ...");
				var dataName = $"{name}.csv";
				var cardData = Path.Combine(cardType.Directory!.FullName, dataName);
				if (!File.Exists(cardData)) {
					Log.Warning($"Card data file {dataName} missing, skipping.");
					continue;
				}

				Template template;
				try {
					var templateText = File.ReadAllText(cardType.FullName);
					template = Template.Parse(templateText);
					if (targetTemplate.HasErrors) throw new InvalidOperationException(targetTemplate.Messages.ToString());
				}
				catch (Exception ex) {
					Log.Warning($"Error parsing card type template. Skipping. Message: {ex.Message}");
					continue;
				}
				var cards = new List<string>();
				using (var reader = new StreamReader(cardData)) {
					try {
						var headers = (await reader.ReadLineAsync()).Split(';');
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
						Log.Error($"Parsing card data: {ex.Message}. Skipping.");
					}
				}

				var cardTypeCss = string.Empty;
				var cssPath = Path.Combine(cardType.Directory.FullName, $"{name}.css");
				if (File.Exists(cssPath)) {
					try {
						cardTypeCss  = File.ReadAllText(cssPath);
						Log.Info($"... Also added css for {name} ...");
					}
					catch (Exception ex) {
						Log.Error($"Adding css: {ex.Message}. Skipping.");
					}
				}

				var scripts = new List<string>();
				foreach (var scriptFile in new DirectoryInfo(scriptsDirectory).EnumerateFiles("*.js")) {
					try {
						var script = File.ReadAllText(scriptFile.FullName);
						scripts.Add(script);
						Log.Info($"... Also added script: {scriptFile.Name} ...");
					}
					catch (Exception ex) {
						Log.Error($"Adding script: {ex.Message}. Skipping.");
					}
				}

				var globalTargetCss = string.Empty;
				var globalTargetCssPath = Path.Combine(targetDirectory, "global.css");
				if (File.Exists(globalTargetCssPath)) {
					try {
						globalTargetCss = File.ReadAllText(globalTargetCssPath);
					}
					catch (Exception ex) {
						Log.Error($"Adding global target css: {ex.Message}. Skipping.");
					}
				}

				var outputPath = Path.Combine(outputDirectory, $"{name}.html");
				try {
					using var output = new StreamWriter(outputPath, false);
					var model = new WrapperModel(parser) {
						name = name,
						cards = cards,
						cardtypecss = cardTypeCss,
						globaltargetcss = globalTargetCss,
						scripts = scripts
					};
					var scriptObject = new ScriptObject();
					scriptObject.Import(model);
					scriptObject.Import("md", new Func<string, string>(s => parser.Parse(s)));
					var outputResult = await targetTemplate.RenderAsync(scriptObject);
					Log.Info($"... created {cards.Count} cards: {outputPath}");
					await output.WriteLineAsync(outputResult);
				}
				catch (Exception ex) {
					Log.Error($"Wring output: {ex.Message}. Skipping.");
				}

				if (image) {
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
								   --headless --screenshot="{Path.Combine(directory, imagePath)}" --window-size="{imageSize}" "{Path.Combine(directory, outputPath)}"
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

						Log.Info($"done: {imagePath}");
					}
					catch (Exception ex) {
						Log.Error($"Generating image: {ex.Message}. Skipping. Command was: \"{exe}\" {args}");
					}
				}
			}
			Log.Success("Finished.");
		}
		catch (Exception ex) {
			Log.Error($"While converting: {ex.Message}");
		}
	}

	[SuppressMessage("ReSharper", "InconsistentNaming")]
	[SuppressMessage("ReSharper", "IdentifierTypo")]
	private class WrapperModel {
		private readonly MarkdownParser Parser;
		public WrapperModel(MarkdownParser parser) {
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