using System.Security.Cryptography;

namespace tuffCards.Tests.Helpers;

public static class IoHelper {
	public static string GetFileHash(this string path) {
		using var sha256 = SHA256.Create();
		using var fileStream = File.OpenRead(path);
		var hashBytes = sha256.ComputeHash(fileStream);
		return Convert.ToHexString(hashBytes);
	}
}