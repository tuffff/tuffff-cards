namespace tuffCards.Markdown;

public class ImageExtension : IMarkdownExtension {
	private readonly ImageOptions Options;
	private readonly ILogger Logger;
	public ImageExtension(ImageOptions options, ILogger logger) {
		Options = options;
		Logger = logger;
	}

	public void Setup(MarkdownPipelineBuilder pipeline) {
		pipeline.InlineParsers.Add(new ImageParser());
	}

	public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer) {
		renderer.ObjectRenderers.Add(new ImageRenderer(Options, Logger));
	}
}
