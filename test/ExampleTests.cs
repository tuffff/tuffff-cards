using test.Helpers;
using TuffCards;
using Xunit.Abstractions;

namespace test;

public class ExampleTests {
	private readonly ITestOutputHelper TestOutputHelper;

	public ExampleTests(ITestOutputHelper testOutputHelper) {
		TestOutputHelper = testOutputHelper;
	}

	[Fact]
	public async Task ValidateExampleDefaultOutput() {
		using var tempDir = new TemporaryDirectory();
		using var _ = new ConsoleCapture(TestOutputHelper);

		await Program.Main(["create-example"]);
		await Program.Main(["convert"]);

		foreach (var (file, expectedHash) in new[] {
				("actions", "B189DEC38A1FCBA5DDD50B96478724E01CDE1F676F2C4B35B78C243090FAE877"),
				("buildings", "12E7ED8866B4E04FBE8D949725A34A2225ED25A5B967BBE71E2A5BDE4C907418")
			}) {
			var path = tempDir.GetPath($@"output\default\{file}.html");
			Assert.True(File.Exists(path), $"The file '{path}' was not created.");
			var actualHash = path.GetFileHash();
			TestOutputHelper.WriteLine($"File {file} has hash {actualHash}");
			Assert.Equal(expectedHash, actualHash);
		}
	}

	[Fact]
	public async Task ValidateExampleSpriteImageOutput() {
		using var tempDir = new TemporaryDirectory();
		using var _ = new ConsoleCapture(TestOutputHelper);

		await Program.Main(["create-example"]);
		await Program.Main(["convert", "--target", "sprite", "--image"]);

		foreach (var (file, expectedHash) in new[] {
			("actions", "1FAB9B62FC6795337100DF17321B5F613E83078090EDD8703F2D817E77DDB658"),
			("buildings", "3D465B67FA12F01EBD573806C095208C7E6B771FE5A9116192CF1DC9FD27416B")
		}) {
			var path = tempDir.GetPath($@"output\sprite\{file}.png");
			Assert.True(File.Exists(path), $"The file '{path}' was not created.");
			var actualHash = path.GetFileHash();
			TestOutputHelper.WriteLine($"File {file} has hash {actualHash}");
			Assert.Equal(expectedHash, actualHash);
		}
	}
}