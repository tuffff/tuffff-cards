using tuffCards.Services;

namespace tuffCards.Markdown;

public class CustomMarkdownParser {
	private readonly ILogger<MarkdownPipeline> Logger;
	private readonly MarkdownPipeline Pipeline;

	public CustomMarkdownParser(FolderRepository folderRepository, string targetName, ILogger<MarkdownPipeline> logger) {
		Logger = logger;
		Pipeline = new MarkdownPipelineBuilder()
			.UseImages(new ImageOptions {
				ImagesPath = folderRepository.GetImageDirectory(),
				IconsPath = folderRepository.GetIconsDirectory(),
				OutputPath = folderRepository.GetOutputDirectory(targetName)
			}, logger)
			.Build();
	}

	public string Parse(string md) {
		var result = Markdig.Markdown.ToHtml(md, Pipeline);
		if (result.LastIndexOf("<p>", StringComparison.InvariantCultureIgnoreCase) == 0 && result.IndexOf("</p>", StringComparison.InvariantCultureIgnoreCase) == result.Length - 5) {
			result = result.Substring(3, result.Length - 8);
		}
		Logger.LogTrace("Converted {md} to {result}", md, result);
		return result;
	}
}