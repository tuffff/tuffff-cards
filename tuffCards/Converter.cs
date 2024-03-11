using Scriban;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace TuffCards;

public static class Converter {
	public static async Task Convert(string wrapper) {
		try {
			var directory = new DirectoryInfo(Environment.CurrentDirectory).FullName;
			var wrapperDirectory = Path.Combine(directory, "wrappers");
			var cardsDirectory = Path.Combine(directory, "cards");
			var outputDirectory = Path.Combine(directory, "output");
			var iconDirectory = Path.Combine(directory, "icons");
			var imageDirectory = Path.Combine(directory, "images");

			Console.WriteLine($"Project directory: {directory}");

			if (!Directory.Exists(wrapperDirectory)) throw new Exception("Directory '/wrappers' not found. Did you create a new project with 'tuffCards create'?");
			if (!Directory.Exists(cardsDirectory)) throw new Exception("Directory '/cards' not found. Did you create a new project with 'tuffCards create'?");
			if (!Directory.Exists(outputDirectory)) Directory.CreateDirectory(outputDirectory);

			var wrapperPath = Path.Combine(wrapperDirectory, wrapper);
			if (!File.Exists(wrapperPath)) throw new Exception($"Wrapper file '{wrapper}' not found. (full path: {wrapperPath})");

			Console.WriteLine($"Using wrapper template: {wrapperPath}");
			Template wrapperTemplate;
			try {
				wrapperTemplate = Template.Parse(File.ReadAllText(wrapperPath));
				if (wrapperTemplate.HasErrors) throw new InvalidOperationException(wrapperTemplate.Messages.ToString());
			}
			catch (Exception ex) {
				throw new Exception($"Error parsing wrapper template: {ex.Message}");
			}

			var parser = new MarkdownParser(iconDirectory, imageDirectory, outputDirectory);
			foreach (var cardType in new DirectoryInfo(cardsDirectory).EnumerateFiles("*.html", SearchOption.AllDirectories)) {
				var name = Path.GetFileNameWithoutExtension(cardType.Name);
				Console.WriteLine($"Card type: {name} ...");
				var dataName = $"{name}.csv";
				var cardData = Path.Combine(cardType.Directory.FullName, dataName);
				if (!File.Exists(cardData)) {
					Console.WriteLine($"Warning: card data file {dataName} missing, skipping.");
					continue;
				}

				Template template;
				try {
					template = Template.Parse(File.ReadAllText(cardType.FullName));
					if (wrapperTemplate.HasErrors) throw new InvalidOperationException(wrapperTemplate.Messages.ToString());
				}
				catch (Exception ex) {
					Console.WriteLine($"Warning: Error parsing card type template. Skipping. Message: {ex.Message}");
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
						var outputPath = Path.Combine(outputDirectory, $"{name}.html");
						using var output = new StreamWriter(outputPath, false);
						var outputResult = await wrapperTemplate.RenderAsync(new { name, cards });
						Console.WriteLine($"... created {cards.Count} cards.");
						await output.WriteLineAsync(outputResult);
					}
					catch (Exception ex) {
						Console.WriteLine($"Error parsing card data: {ex.Message}. Skipping.");
					}
				}
			}
			Console.WriteLine("Finished.");
		}
		catch (Exception ex) {
			Console.Error.WriteLine($"Error converting: {ex.Message}");
		}
	}
}