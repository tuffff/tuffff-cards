using System.Runtime.CompilerServices;

namespace test;

public class TemporaryDirectory : IDisposable {
	public string FolderPath { get; }
	private readonly string OldCurrentDirectory;

	public TemporaryDirectory([CallerFilePath] string classPath = "", [CallerMemberName] string methodName = "") {
		var className = Path.GetFileNameWithoutExtension(classPath);
		FolderPath = Path.Combine(Path.GetTempPath(), $"tuffCards-{className}-{methodName}-{Guid.NewGuid().ToString()}");
		Directory.CreateDirectory(FolderPath);
		OldCurrentDirectory = Directory.GetCurrentDirectory();
		Directory.SetCurrentDirectory(FolderPath);
	}

	public string GetPath(string relativePath) {
		return Path.Combine(FolderPath, relativePath);
	}

	public void Dispose() {
		Directory.SetCurrentDirectory(OldCurrentDirectory);
		Directory.Delete(FolderPath, true);
	}
}