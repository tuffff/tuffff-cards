using tuffCards.Tests.Helpers;

namespace tuffCards.Tests.Integration;

[Collection(Collections.UsesCwd)]
public class ExampleTests(ITestOutputHelper TestOutputHelper, TemporaryDirectoryFactory TemporaryDirectoryFactory) {

	[Fact]
	[UsedImplicitly]
	public async Task ValidateExampleDefaultOutput() {
		using var tempDir = TemporaryDirectoryFactory.Create();
		using var _ = new ConsoleCapture(TestOutputHelper);

		await Program.Main(["create-example"]);
		await Program.Main(["convert"]);

		foreach (var (file, expectedHash) in new[] {
				("actions", "DB046EF506132317C91103CA2B7FAFEF9455ACD04216EDA77C3244910919A122"),
				("buildings", "6371EFA8DBDE429FF11848AE939D42110BBFA65B6E19A0296631040163FA1692")
			}) {
			var path = tempDir.GetPath($@"output\default\{file}.html");
			Assert.True(File.Exists(path), $"The file '{path}' was not created.");
			var actualHash = path.GetFileHash();
			TestOutputHelper.WriteLine($"File {file} has hash {actualHash}");
			Assert.Equal(expectedHash, actualHash);
		}
	}

	[Fact]
	[UsedImplicitly]
	public async Task ValidateExampleSpriteImageOutput() {
		using var tempDir = TemporaryDirectoryFactory.Create();
		using var _ = new ConsoleCapture(TestOutputHelper);

		await Program.Main(["create-example"]);
		await Program.Main(["convert", "--target", "sprite", "--image"]);

		foreach (var (file, expectedHash) in new[] {
			("actions", "5387584E616D2DF64C4266BE0752D7DAD12E2EADEFF325615B66BD8B024DA740"),
			("buildings", "5140D4F70770A285A6631363B124C5D34C368D6F3ACC3C817F0279C7F4A3366A")
		}) {
			var path = tempDir.GetPath($@"output\sprite\{file}.png");
			Assert.True(File.Exists(path), $"The file '{path}' was not created.");
			var actualHash = path.GetFileHash();
			TestOutputHelper.WriteLine($"File {file} has hash {actualHash}");
			Assert.Equal(expectedHash, actualHash);
		}
	}

	[Fact]
	[UsedImplicitly]
	public async Task ValidateExampleSingleBleedSpriteImageOutput() {
		using var tempDir = TemporaryDirectoryFactory.Create();
		using var _ = new ConsoleCapture(TestOutputHelper);

		await Program.Main(["create-example"]);
		await Program.Main(["convert", "--single", "--image", "--target", "sprite", "--bleed", "3mm"]);

		foreach (var (file, expectedHash) in new[] {
			("actions Do it hard", "CCFC6E216B5B450BFD43C27F14FD8DC90769A55BD5096BFFCDDD7CC089B30B0F"),
			("buildings The great, awesome Castle of TuffVille", "ACD37990C3EC0B2FDE3A22170E0FAADA5144CE8854EC700239C19B98E5610B68")
		}) {
			var path = tempDir.GetPath($@"output\sprite\{file}.png");
			Assert.True(File.Exists(path), $"The file '{path}' was not created.");
			var actualHash = path.GetFileHash();
			TestOutputHelper.WriteLine($"File {file} has hash {actualHash}");
			Assert.Equal(expectedHash, actualHash);
		}
	}
}