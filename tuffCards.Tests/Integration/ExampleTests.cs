using tuffCards.Tests.Helpers;

namespace tuffCards.Tests.Integration;

[Collection(Collections.UsesCwd)]
public class ExampleTests(ITestOutputHelper testOutputHelper, TemporaryDirectoryFactory temporaryDirectoryFactory) {

	[Fact]
	[UsedImplicitly]
	public async Task ValidateExampleDefaultOutput() {
		using var tempDir = temporaryDirectoryFactory.Create();
		using var _ = new ConsoleCapture(testOutputHelper);

		await Program.Main(["create-example"]);
		await Program.Main(["convert"]);

		foreach (var (file, expectedHash) in new[] {
				("actions", "F30540C75D9D9D506AEE72BC975B407086D6D755E23CD92D2B26B7FD5A92C076"),
				("buildings", "84F0C36B249B29BB818A12C1072AFF167A9907795EA33B0172F8806E0E6D1731")
			}) {
			var path = tempDir.GetPath($@"output\default\{file}.html");
			Assert.True(File.Exists(path), $"The file '{path}' was not created.");
			var actualHash = path.GetFileHash();
			testOutputHelper.WriteLine($"File {file} has hash {actualHash}");
			Assert.Equal(expectedHash, actualHash);
		}
	}

	[Fact]
	[UsedImplicitly]
	public async Task ValidateExampleSpriteImageOutput() {
		using var tempDir = temporaryDirectoryFactory.Create();
		using var _ = new ConsoleCapture(testOutputHelper);

		await Program.Main(["create-example"]);
		await Program.Main(["convert", "--target", "sprite", "--image"]);

		foreach (var (file, expectedHash) in new[] {
			("actions", "1FAB9B62FC6795337100DF17321B5F613E83078090EDD8703F2D817E77DDB658"),
			("buildings", "3D465B67FA12F01EBD573806C095208C7E6B771FE5A9116192CF1DC9FD27416B")
		}) {
			var path = tempDir.GetPath($@"output\sprite\{file}.png");
			Assert.True(File.Exists(path), $"The file '{path}' was not created.");
			var actualHash = path.GetFileHash();
			testOutputHelper.WriteLine($"File {file} has hash {actualHash}");
			Assert.Equal(expectedHash, actualHash);
		}
	}
}