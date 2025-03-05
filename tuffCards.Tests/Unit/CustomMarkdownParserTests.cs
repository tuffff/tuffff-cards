using tuffCards.Markdown;
using tuffCards.Services;

namespace tuffCards.Tests.Unit;

[Collection(Collections.UsesCwd)]
public class CustomMarkdownParserTests(MarkdownParserFactory ParserFactory, FolderRepository FolderRepository) {

	private readonly CustomMarkdownParser Parser = ParserFactory.Build("default");

	[Fact]
	[UsedImplicitly]
	public void StripsOutRootPTag() {
		const string testString = "test";
		var result = Parser.Parse(testString);
		Assert.Equal(testString, result);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	[UsedImplicitly]
	public void RendersIcons(bool useImage) {
		var directory = useImage ? FolderRepository.GetImageDirectory() : FolderRepository.GetIconsDirectory();
		const string imageName = "test";
		const string fileName = $"{imageName}.png";
		Assert.True(Directory.Exists(directory));

		var testFilePath = Path.Combine(directory, fileName);
		var outputFilePath = Path.Combine(FolderRepository.GetOutputDirectory("default"), $"{(useImage ? "image" : "icon")}-{fileName}");
		using (var _ = File.Create(testFilePath)) {}

		var parsedResult = Parser.Parse(useImage ? $"{{{{{imageName}}}}}" : $"{{{imageName}}}");
		Assert.Contains(fileName, parsedResult);
		Assert.True(File.Exists(outputFilePath));

		File.Delete(testFilePath);
		File.Delete(outputFilePath);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	[UsedImplicitly]
	public void RendersIconsWithMissingFileAsText(bool useImage) {
		var parsedResult = Parser.Parse(useImage ? "{{missing}}" : "{missing}");
		Assert.Equal($"""<em class="{(useImage ? "image" : "icon")}">missing</em>""", parsedResult);
	}
}