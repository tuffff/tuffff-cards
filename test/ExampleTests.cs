using TuffCards;

namespace test;

public class ExampleTests {
	[Fact]
	public async Task ValidateExampleDefaultOutput() {
		using var tempDir = new TemporaryDirectory();
		await Program.Main(["create-example"]);
		await Program.Main(["convert"]);

		foreach (var (file, hash) in new[] {
				("actions", "B189DEC38A1FCBA5DDD50B96478724E01CDE1F676F2C4B35B78C243090FAE877"),
				("buildings", "12E7ED8866B4E04FBE8D949725A34A2225ED25A5B967BBE71E2A5BDE4C907418")
			}) {
			var path = tempDir.GetPath($@"output\default\{file}.html");
			Assert.True(File.Exists(path), $"The file '{path}' was not created.");
			Assert.Equal(hash, path.GetFileHash());
		}
	}

	[Fact]
	public async Task ValidateExampleSpriteImageOutput() {
		using var tempDir = new TemporaryDirectory();
		await Program.Main(["create-example"]);
		await Program.Main(["convert", "--target", "sprite", "--image"]);

		foreach (var (file, hash) in new[] {
			("actions", "6C457056669FA47E1821EBFADADDC4E2ED9A8CC552CA3554E9F41F23C226D7BE"),
			("buildings", "13D00CCD71283C3EE58BA10BBD89830FF155D1B96AE7CE2821320751F9D85E1E")
		}) {
			var path = tempDir.GetPath($@"output\sprite\{file}.png");
			Assert.True(File.Exists(path), $"The file '{path}' was not created.");
			Assert.Equal(hash, path.GetFileHash());
		}
	}
}