namespace tuffCards.Markdown;

public static class MarkdownPipelineExtension {
	public static MarkdownPipelineBuilder UseImages(this MarkdownPipelineBuilder builder, ImageOptions options, ILogger logger) {
		if (!builder.Extensions.Contains<ImageExtension>()) {
			builder.Extensions.Add(new ImageExtension(options, logger));
		}
		return builder;
	}
}
