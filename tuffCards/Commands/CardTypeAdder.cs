using tuffCards.Repositories;

namespace tuffCards.Commands;

public class CardTypeAdder {
	private readonly FolderRepository FolderRepository;
	private readonly ILogger<CardTypeAdder> Logger;

	public CardTypeAdder(FolderRepository folderRepository, ILogger<CardTypeAdder> logger) {
		FolderRepository = folderRepository;
		Logger = logger;
	}

	public Task Add(string name, bool force) {
		var cardsDirectory = FolderRepository.GetCardsDirectory();
		var htmlPath = Path.Combine(cardsDirectory, $"{name}.html");
		var csvPath = Path.Combine(cardsDirectory, $"{name}.csv");
		var cssPath = Path.Combine(cardsDirectory, $"{name}.css");
		if (!force && (File.Exists(htmlPath) || File.Exists(csvPath) || File.Exists(cssPath))) {
			Logger.LogError("Card type {cardTypeName} already exists. Use --force to overwrite.", name);
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
			Logger.LogSuccess("Card type {name} added successfully.", name);
		}
		return Task.CompletedTask;
	}
}