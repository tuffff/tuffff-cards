using Scriban;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TuffCards;

public static class Converter {
	public static async Task Convert(string wrapper, bool screenshot) {
		try {
			var directory = new DirectoryInfo(Environment.CurrentDirectory).FullName;
			var wrapperDirectory = Path.Combine(".", "wrappers");
			var cardsDirectory = Path.Combine(".", "cards");
			var outputRootDirectory = Path.Combine(".", "output");
			var outputDirectory = Path.Combine(outputRootDirectory, wrapper);
			var iconDirectory = Path.Combine(".", "icons");
			var imageDirectory = Path.Combine(".", "images");
			var scriptsDirectory = Path.Combine(".", "scripts");

			Log.Info($"Project directory: {directory}");

			if (!Directory.Exists(wrapperDirectory)) throw new Exception("Directory '/wrappers' not found. Did you create a new project with 'tuffCards create'?");
			if (!Directory.Exists(cardsDirectory)) throw new Exception("Directory '/cards' not found. Did you create a new project with 'tuffCards create'?");
			if (!Directory.Exists(outputRootDirectory)) Directory.CreateDirectory(outputDirectory);
			if (!Directory.Exists(outputDirectory)) Directory.CreateDirectory(outputDirectory);
			var outputDirectoryInfo = new DirectoryInfo(outputDirectory);
			foreach (var file in outputDirectoryInfo.GetFiles()) file.Delete();
			foreach (var dir in outputDirectoryInfo.GetDirectories()) dir.Delete(true);

			var wrapperPath = Path.Combine(wrapperDirectory, $"{wrapper}.html");
			if (!File.Exists(wrapperPath)) throw new Exception($"Wrapper file '{wrapper}' not found. (path: {wrapperPath})");

			Log.Info($"Using wrapper template: {wrapperPath}");
			string wrapperTemplateText;
			Template wrapperTemplate;
			try {
				wrapperTemplateText = File.ReadAllText(wrapperPath);
				wrapperTemplate = Template.Parse(wrapperTemplateText);
				if (wrapperTemplate.HasErrors) throw new InvalidOperationException(wrapperTemplate.Messages.ToString());
			}
			catch (Exception ex) {
				throw new Exception($"Error parsing wrapper template: {ex.Message}");
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
					if (wrapperTemplate.HasErrors) throw new InvalidOperationException(wrapperTemplate.Messages.ToString());
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
							var result = await template.RenderAsync(data);
							cards.Add(result);
						}
					}
					catch (Exception ex) {
						Log.Error($"Parsing card data: {ex.Message}. Skipping.");
					}
				}

				var cardtypecss = string.Empty;
				var cssPath = Path.Combine(cardType.Directory.FullName, $"{name}.css");
				if (File.Exists(cssPath)) {
					try {
						cardtypecss  = File.ReadAllText(cssPath);
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

				var globalwrappercss = string.Empty;
				var globalWrapperCssPath = Path.Combine(wrapperDirectory, "global.css");
				if (File.Exists(globalWrapperCssPath)) {
					try {
						globalwrappercss = File.ReadAllText(globalWrapperCssPath);
					}
					catch (Exception ex) {
						Log.Error($"Adding global wrapper css: {ex.Message}. Skipping.");
					}
				}

				var outputPath = Path.Combine(outputDirectory, $"{name}.html");
				try {
					using var output = new StreamWriter(outputPath, false);
					var outputResult = await wrapperTemplate.RenderAsync(new { name, cards, cardtypecss, globalwrappercss, scripts });
					Log.Info($"... created {cards.Count} cards: {outputPath}");
					await output.WriteLineAsync(outputResult);
				}
				catch (Exception ex) {
					Log.Error($"Wring output: {ex.Message}. Skipping.");
				}

				if (screenshot) {
					Console.Write("Screenshot ... ");
					var screenshotPath = Path.Combine(outputDirectory, $"{name}.png");
					var screenshotSize = "1000,1000";
					var match = Regex.Match(wrapperTemplateText, @$"<!-- screenshot-size-{name}:(\d+)x(\d+) -->");
					if (match.Success) {
						screenshotSize = $"{match.Groups[1].Value},{match.Groups[2].Value}";
					}
					else {
						match = Regex.Match(wrapperTemplateText, @$"<!-- screenshot-size:(\d+)x(\d+) -->");
						if (match.Success) {
							screenshotSize = $"{match.Groups[1].Value},{match.Groups[2].Value}";
						}
					}
					const string exe = """
					                    C:\Program Files (x86)\Google\Chrome\Application\chrome.exe
					                    """;
					var args = $"""
								   --headless --screenshot="{Path.Combine(directory, screenshotPath)}" --window-size="{screenshotSize}" "{Path.Combine(directory, outputPath)}"
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

						Log.Info($"done: {screenshotPath}");
					}
					catch (Exception ex) {
						Log.Error($"Taking screenshot: {ex.Message}. Skipping. Command was: \"{exe}\" {args}");
					}
				}
			}
			Log.Success("Finished.");
		}
		catch (Exception ex) {
			Log.Error($"While converting: {ex.Message}");
		}
	}
}