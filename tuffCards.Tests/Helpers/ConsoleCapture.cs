namespace tuffCards.Tests.Helpers;

public sealed class ConsoleCapture : IDisposable {
	private readonly ITestOutputHelper TestOutputHelper;
	private readonly StringWriter ConsoleOutput;
	private readonly StringWriter ConsoleError;

	public ConsoleCapture(ITestOutputHelper testOutputHelper) {
		TestOutputHelper = testOutputHelper;
		ConsoleOutput = new StringWriter();
		ConsoleError = new StringWriter();
		Console.SetOut(ConsoleOutput);
		Console.SetError(ConsoleError);
	}

	public void Dispose() {
		var output = ConsoleOutput.ToString();
		if (output.Length > 0) {
			TestOutputHelper.WriteLine("Console output:");
			TestOutputHelper.WriteLine(output);
		}
		var error = ConsoleError.ToString();
		if (error.Length > 0) {
			TestOutputHelper.WriteLine("Console error:");
			TestOutputHelper.WriteLine(error);
		}
		ConsoleOutput.Dispose();
		ConsoleError.Dispose();
	}
}