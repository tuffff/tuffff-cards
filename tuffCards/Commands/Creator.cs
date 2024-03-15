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

			var targetsDir = Path.Combine(directory, "targets");
			Directory.CreateDirectory(targetsDir);
			File.WriteAllText(Path.Combine(targetsDir, "global.css"), Presets.GlobalTargetCss);
			File.WriteAllText(Path.Combine(targetsDir, "default.html"), Presets.DefaultTarget);

			var cardsDir = Path.Combine(directory, "cards");
			Directory.CreateDirectory(cardsDir);

			var imagesDir = Path.Combine(directory, "images");
			Directory.CreateDirectory(imagesDir);

			var iconsDir = Path.Combine(directory, "icons");
			Directory.CreateDirectory(iconsDir);

			var scriptsDir = Path.Combine(directory, "scripts");
			Directory.CreateDirectory(scriptsDir);
			File.WriteAllText(Path.Combine(scriptsDir, "fit-text.js"), Presets.FitTextScript);

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

			var targetsDir = Path.Combine(directory, "targets");
			Directory.CreateDirectory(targetsDir);
			File.WriteAllText(Path.Combine(targetsDir, "global.css"), Presets.GlobalTargetCss);
			File.WriteAllText(Path.Combine(targetsDir, "default.html"), Presets.DefaultTarget);
			File.WriteAllText(Path.Combine(targetsDir, "tts.html"), Presets.TtsTarget);

			var cardsDir = Path.Combine(directory, "cards");
			Directory.CreateDirectory(cardsDir);
			File.WriteAllText(Path.Combine(cardsDir, "actions.html"), Presets.DefaultActions);
			File.WriteAllText(Path.Combine(cardsDir, "actions.csv"), Presets.DefaultActionsData);
			File.WriteAllText(Path.Combine(cardsDir, "actions.css"), Presets.DefaultActionsCss);
			File.WriteAllText(Path.Combine(cardsDir, "buildings.html"), Presets.DefaultBuildings);
			File.WriteAllText(Path.Combine(cardsDir, "buildings.csv"), Presets.DefaultBuildingsData);
			File.WriteAllText(Path.Combine(cardsDir, "buildings.css"), Presets.DefaultBuildingsCss);

			var imagesDir = Path.Combine(directory, "images");
			Directory.CreateDirectory(imagesDir);
			File.WriteAllText(Path.Combine(imagesDir, "strong.svg"), Presets.StrongImage);

			var iconsDir = Path.Combine(directory, "icons");
			Directory.CreateDirectory(iconsDir);
			File.WriteAllText(Path.Combine(iconsDir, "tap.svg"), Presets.TapImage);

			var scriptsDir = Path.Combine(directory, "scripts");
			Directory.CreateDirectory(scriptsDir);
			File.WriteAllText(Path.Combine(scriptsDir, "fit-text.js"), Presets.FitTextScript);

			Logger.LogSuccess("Example project created. Run 'tuffCards convert' to see an output.");
		}
		catch (Exception ex) {
			Logger.LogError("While creating: {exceptionMessage}", ex.Message);
		}
		return Task.CompletedTask;
	}
}