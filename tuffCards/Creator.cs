using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace TuffCards;

public static class Creator {
	public static Task Create(bool force) {
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

			Log.Success("Project created. Run 'tuffCards add-type <name>' to add some cards.");
		}
		catch (Exception ex) {
			Log.Error($"While creating: {ex.Message}");
		}
		return Task.CompletedTask;
	}

	public static Task CreateExample(bool force) {
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

			Log.Success("Example project created. Run 'tuffCards convert' to see an output.");
		}
		catch (Exception ex) {
			Log.Error($"While creating: {ex.Message}");
		}
		return Task.CompletedTask;
	}
}