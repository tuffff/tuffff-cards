namespace tuffCards.Repositories;

public class FolderRepository {
	private readonly string TargetDirectory;
	public string GetTargetDirectory() {
		Directory.CreateDirectory(TargetDirectory);
		return TargetDirectory;
	}

	private readonly string CardDirectory;
	public string GetCardsDirectory() {
		Directory.CreateDirectory(CardDirectory);
		return CardDirectory;
	}

	private readonly string OutputRootDirectory;
	public string GetOutputRootDirectory() {
		Directory.CreateDirectory(OutputRootDirectory);
		return OutputRootDirectory;
	}

	public string GetOutputDirectory(string targetName) {
		var result = Path.Combine(OutputRootDirectory, targetName);
		Directory.CreateDirectory(result);
		return result;
	}

	public void ClearOutputDirectory(string targetName) {
		var directory = Path.Combine(OutputRootDirectory, targetName);
		var directoryInfo = new DirectoryInfo(directory);
		if (!directoryInfo.Exists) return;
		foreach (var file in directoryInfo.GetFiles()) file.Delete();
		foreach (var dir in directoryInfo.GetDirectories()) dir.Delete(true);
	}

	private readonly string IconDirectory;
	public string GetIconsDirectory() {
		Directory.CreateDirectory(IconDirectory);
		return IconDirectory;
	}

	private readonly string ImageDirectory;
	public string GetImageDirectory() {
		Directory.CreateDirectory(ImageDirectory);
		return ImageDirectory;
	}

	private readonly string ScriptsDirectory;
	public string GetScriptsDirectory() {
		Directory.CreateDirectory(ScriptsDirectory);
		return ScriptsDirectory;
	}

	public bool IsValidRootFolder() {
		return Directory.Exists(TargetDirectory) && Directory.Exists(CardDirectory);
	}

	public FolderRepository(string rootPath) {
		TargetDirectory = Path.Combine(rootPath, "targets");
		CardDirectory = Path.Combine(rootPath, "cards");
		OutputRootDirectory = Path.Combine(rootPath, "output");
		IconDirectory = Path.Combine(rootPath, "icons");
		ImageDirectory = Path.Combine(rootPath, "images");
		ScriptsDirectory = Path.Combine(rootPath, "scripts");
	}
}