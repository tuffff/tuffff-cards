using TuffCards;

namespace test;

public class SmokeTest {
	[Fact]
	public async Task MinimalExample() {
		var tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		Directory.CreateDirectory(tempFolder);
		Directory.SetCurrentDirectory(tempFolder);

		try {
			await Program.Main(["create-example"]);
			await Program.Main(["convert"]);

			var testFilePath = Path.Combine(tempFolder, @"output\default\actions.html");

			Assert.True(File.Exists(testFilePath), $"The file '{testFilePath}' was not created.");
			Assert.True(new FileInfo(testFilePath).Length > 5000, $"The file '{testFilePath}' is too small.");
		}
		finally {
			if (Directory.Exists(tempFolder)) {
				// Directory.Delete(tempFolder, true);
			}
		}
	}
}