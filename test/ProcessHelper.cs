using System.Diagnostics;

namespace test;

public static class ProcessHelper {
	public static ProcessResult Run(string folder, string command) {
		var result = new ProcessResult();

		var startInfo = new ProcessStartInfo {
			FileName = "cmd.exe",
			Arguments = $"/C {command}",
			WorkingDirectory = folder,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true
		};

		// Start the process
		using var process = Process.Start(startInfo);

		if (process == null) {
			throw new InvalidOperationException("Failed to start the process");
		}

		// Read the process output and error streams
		result.StandardOutput = process.StandardOutput.ReadToEnd();
		result.StandardError = process.StandardError.ReadToEnd();

		// Wait for the process to complete
		process.WaitForExit();

		Assert.True(process.ExitCode == 0, $"Process exited with code {process.ExitCode}.");

		return result;
	}

	public class ProcessResult {
		public string StandardOutput { get; set; } = string.Empty;
		public string StandardError { get; set; } = string.Empty;
	}
}