using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace TuffCards;

public static class Creator {
	public static async Task Create(bool force) {
		try {
			var directory = new DirectoryInfo(Environment.CurrentDirectory).FullName;
			if (!force && Directory.EnumerateFileSystemEntries(directory).Any())
				throw new Exception("Current folder is not empty. Use --force to create anyway.");

			var wrappersDir = Path.Combine(directory, "wrappers");
			var defaultWrapper = Path.Combine(wrappersDir, "default.html");
			var cardsDir = Path.Combine(directory, "cards");
			var actionsCard = Path.Combine(cardsDir, "actions.html");
			var actionsCardData = Path.Combine(cardsDir, "actions.csv");
			var actionsCardCss = Path.Combine(cardsDir, "actions.css");
			var buildingsCard = Path.Combine(cardsDir, "buildings.html");
			var buildingsCardData = Path.Combine(cardsDir, "buildings.csv");
			var buildingsCardCss = Path.Combine(cardsDir, "buildings.css");
			var imagesDir = Path.Combine(directory, "images");
			var strongImage = Path.Combine(imagesDir, "strong.svg");
			var iconsDir = Path.Combine(directory, "icons");
			var tapIcon = Path.Combine(iconsDir, "tap.svg");

			Directory.CreateDirectory(wrappersDir);
			Directory.CreateDirectory(cardsDir);
			Directory.CreateDirectory(imagesDir);
			Directory.CreateDirectory(iconsDir);

			using (var writer = new StreamWriter(defaultWrapper))
				await writer.WriteLineAsync(Presets.DefaultWrapper);
			using (var writer = new StreamWriter(actionsCard))
				await writer.WriteLineAsync(Presets.DefaultActions);
			using (var writer = new StreamWriter(actionsCardData))
				await writer.WriteLineAsync(Presets.DefaultActionsData);
			using (var writer = new StreamWriter(actionsCardCss))
				await writer.WriteLineAsync(Presets.DefaultActionsCss);
			using (var writer = new StreamWriter(buildingsCard))
				await writer.WriteLineAsync(Presets.DefaultBuildings);
			using (var writer = new StreamWriter(buildingsCardData))
				await writer.WriteLineAsync(Presets.DefaultBuildingsData);
			using (var writer = new StreamWriter(buildingsCardCss))
				await writer.WriteLineAsync(Presets.DefaultBuildingsCss);
			using (var writer = new StreamWriter(strongImage))
				await writer.WriteLineAsync(Presets.StrongImage);
			using (var writer = new StreamWriter(tapIcon))
				await writer.WriteLineAsync(Presets.TapImage);

			Log.Success("Project created. Run 'tuffCards convert' to see an output.");
		}
		catch (Exception ex) {
			Log.Error($"While creating: {ex.Message}");
		}
	}
}