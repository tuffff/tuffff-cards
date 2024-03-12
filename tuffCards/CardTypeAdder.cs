using System.IO;
using System.Threading.Tasks;

namespace TuffCards;

public static class CardTypeAdder {
	public static Task Add(string name, bool force) {
		var cardsDirectory = Path.Combine(".", "cards");
		var htmlPath = Path.Combine(cardsDirectory, $"{name}.html");
		var csvPath = Path.Combine(cardsDirectory, $"{name}.csv");
		var cssPath = Path.Combine(cardsDirectory, $"{name}.css");
		if (!force && (File.Exists(htmlPath) || File.Exists(csvPath) || File.Exists(cssPath))) {
			Log.Error($"Card type {name} already exists. Use --force to overwrite.");
		}
		else {
			Directory.CreateDirectory(cardsDirectory);
			File.WriteAllText(htmlPath, """
										<div class="card" style="background: {{ Color }}">
										    <div class="name">{{ Name }}: {{ Cost }}</div>
										</div>
										""");
			File.WriteAllText(csvPath, "Name;Cost\r\nCard 1;1\r\nCard 2;2");
			File.WriteAllText(cssPath, """
			                           .card {
			                               width: 200px;
			                               height: 300px;
			                               background: #dca;
			                               position: relative;
			                           }
			                           .name {
			                               background: rgba(0, 0, 0, 0.5);
			                               color: white;
			                               position: absolute;
			                               top: 10px;
			                               left: 10px;
			                           }
			                           """);
			Log.Success($"Card type {name} added successfully.");
		}
		return Task.CompletedTask;
	}
}