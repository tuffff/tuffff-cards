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
			File.WriteAllText(Path.Combine(targetsDir, "global.css"), Defaults.GlobalTargetCss);
			File.WriteAllText(Path.Combine(targetsDir, "default.html"), Defaults.DefaultTarget);
			File.WriteAllText(Path.Combine(targetsDir, "sprite.html"), Defaults.SpriteTarget);

			FolderRepository.GetCardsDirectory();
			FolderRepository.GetImageDirectory();
			FolderRepository.GetIconsDirectory();

			var scriptsDir = FolderRepository.GetScriptsDirectory();
			File.WriteAllText(Path.Combine(scriptsDir, "fit-text.js"), Defaults.FitTextScript);

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
			File.WriteAllText(Path.Combine(targetsDir, "global.css"), Defaults.GlobalTargetCss);
			File.WriteAllText(Path.Combine(targetsDir, "default.html"), Defaults.DefaultTarget);
			File.WriteAllText(Path.Combine(targetsDir, "sprite.html"), Defaults.SpriteTarget);

			var cardsDir = FolderRepository.GetCardsDirectory();
			File.WriteAllText(Path.Combine(cardsDir, "actions.html"), Defaults.ExampleActions);
			File.WriteAllText(Path.Combine(cardsDir, "actions.csv"), Defaults.ExampleActionsData);
			File.WriteAllText(Path.Combine(cardsDir, "actions.css"), Defaults.ExampleActionsCss);
			File.WriteAllText(Path.Combine(cardsDir, "buildings.html"), Defaults.ExampleBuildings);
			File.WriteAllText(Path.Combine(cardsDir, "buildings.csv"), Defaults.ExampleBuildingsData);
			File.WriteAllText(Path.Combine(cardsDir, "buildings.css"), Defaults.ExampleBuildingsCss);

			var imagesDir = FolderRepository.GetImageDirectory();
			File.WriteAllText(Path.Combine(imagesDir, "strong.svg"), Defaults.StrongImage);

			var iconsDir = FolderRepository.GetIconsDirectory();
			File.WriteAllText(Path.Combine(iconsDir, "tap.svg"), Defaults.TapImage);

			var scriptsDir = FolderRepository.GetScriptsDirectory();
			File.WriteAllText(Path.Combine(scriptsDir, "fit-text.js"), Defaults.FitTextScript);

			Logger.LogSuccess("Example project created. Run 'tuffCards convert' to see an output.");
		}
		catch (Exception ex) {
			Logger.LogError("While creating: {exceptionMessage}", ex.Message);
		}
		return Task.CompletedTask;
	}
}