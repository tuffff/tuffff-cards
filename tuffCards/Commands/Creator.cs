using tuffCards.Repositories;

namespace tuffCards.Commands;

public class Creator {
	private readonly FolderRepository FolderRepository;
	private readonly ILogger<FolderRepository> Logger;

	public Creator(FolderRepository folderRepository, ILogger<FolderRepository> logger) {
		FolderRepository = folderRepository;
		Logger = logger;
	}

	public Task Create(bool force) {
		try {
			var directory = new DirectoryInfo(Environment.CurrentDirectory).FullName;
			if (!force && Directory.EnumerateFileSystemEntries(directory).Any())
				throw new Exception("Current folder is not empty. Use --force to create anyway.");

			var targetsDir = FolderRepository.GetTargetDirectory();
			File.WriteAllText(Path.Combine(targetsDir, "global.css"), Presets.Presets.GlobalTargetCss);
			File.WriteAllText(Path.Combine(targetsDir, "default.html"), Presets.Presets.DefaultTarget);
			File.WriteAllText(Path.Combine(targetsDir, "sprite.html"), Presets.Presets.SpriteTarget);

			FolderRepository.GetCardsDirectory();
			FolderRepository.GetImageDirectory();
			FolderRepository.GetIconsDirectory();

			var scriptsDir = FolderRepository.GetScriptsDirectory();
			File.WriteAllText(Path.Combine(scriptsDir, "fit-text.js"), Presets.Presets.FitTextScript);

			Logger.LogSuccess("Project created. Run 'tuffCards add-type <name>' to add some cards.");
		}
		catch (Exception ex) {
			Logger.LogError("While creating: {exceptionMessage}", ex.Message);
		}
		return Task.CompletedTask;
	}

	public Task CreateExample(bool force) {
		try {
			var directory = new DirectoryInfo(Environment.CurrentDirectory).FullName;
			if (!force && Directory.EnumerateFileSystemEntries(directory).Any())
				throw new Exception("Current folder is not empty. Use --force to create anyway.");

			var targetsDir = FolderRepository.GetTargetDirectory();
			File.WriteAllText(Path.Combine(targetsDir, "global.css"), Presets.Presets.GlobalTargetCss);
			File.WriteAllText(Path.Combine(targetsDir, "default.html"), Presets.Presets.DefaultTarget);
			File.WriteAllText(Path.Combine(targetsDir, "sprite.html"), Presets.Presets.SpriteTarget);

			var cardsDir = FolderRepository.GetCardsDirectory();
			File.WriteAllText(Path.Combine(cardsDir, "actions.html"), Presets.Presets.DefaultActions);
			File.WriteAllText(Path.Combine(cardsDir, "actions.csv"), Presets.Presets.DefaultActionsData);
			File.WriteAllText(Path.Combine(cardsDir, "actions.css"), Presets.Presets.DefaultActionsCss);
			File.WriteAllText(Path.Combine(cardsDir, "buildings.html"), Presets.Presets.DefaultBuildings);
			File.WriteAllText(Path.Combine(cardsDir, "buildings.csv"), Presets.Presets.DefaultBuildingsData);
			File.WriteAllText(Path.Combine(cardsDir, "buildings.css"), Presets.Presets.DefaultBuildingsCss);

			var imagesDir = FolderRepository.GetImageDirectory();
			File.WriteAllText(Path.Combine(imagesDir, "strong.svg"), Presets.Presets.StrongImage);

			var iconsDir = FolderRepository.GetIconsDirectory();
			File.WriteAllText(Path.Combine(iconsDir, "tap.svg"), Presets.Presets.TapImage);

			var scriptsDir = FolderRepository.GetScriptsDirectory();
			File.WriteAllText(Path.Combine(scriptsDir, "fit-text.js"), Presets.Presets.FitTextScript);

			Logger.LogSuccess("Example project created. Run 'tuffCards convert' to see an output.");
		}
		catch (Exception ex) {
			Logger.LogError("While creating: {exceptionMessage}", ex.Message);
		}
		return Task.CompletedTask;
	}
}