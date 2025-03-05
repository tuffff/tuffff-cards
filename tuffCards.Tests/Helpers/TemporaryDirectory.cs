using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace tuffCards.Tests.Helpers;

public sealed class TemporaryDirectory : IDisposable {
	private readonly ILogger<TemporaryDirectory> Logger;
	private readonly string FolderPath;
	private readonly string OldCurrentDirectory;

	public TemporaryDirectory(ILogger<TemporaryDirectory> logger, [CallerFilePath] string classPath = "", [CallerMemberName] string methodName = "") {
		Logger = logger;
		var className = Path.GetFileNameWithoutExtension(classPath);
		FolderPath = Path.Combine(Path.GetTempPath(), $"tuffCards-{className}-{methodName}-{Guid.NewGuid().ToString()}");
		Directory.CreateDirectory(FolderPath);
		OldCurrentDirectory = Directory.GetCurrentDirectory();
		Directory.SetCurrentDirectory(FolderPath);
		Logger.LogInformation("Entered temporary directory {path}, contains {count} elements", FolderPath, Directory.EnumerateFileSystemEntries(FolderPath).Count());
	}

	public string GetPath(string relativePath) {
		return Path.Combine(FolderPath, relativePath);
	}

	public void Dispose() {
		Directory.SetCurrentDirectory(OldCurrentDirectory);
		Logger.LogInformation("Exited temporary directory, now in {}", FolderPath);
		// TODO: include this
		// Directory.Delete(FolderPath, true);
	}
}

public sealed class TemporaryDirectoryFactory(ILoggerFactory LoggerFactory) {
	public TemporaryDirectory Create() {
		return new TemporaryDirectory(LoggerFactory.CreateLogger<TemporaryDirectory>());
	}
}