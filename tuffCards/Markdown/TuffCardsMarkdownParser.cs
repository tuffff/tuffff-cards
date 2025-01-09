using tuffCards.Repositories;

namespace tuffCards.Markdown;

public class TuffCardsMarkdownParser {
	private readonly ILogger<TuffCardsMarkdownParser> Logger;
	private readonly MarkdownPipeline Pipeline;

	public TuffCardsMarkdownParser(FolderRepository folderRepository, string targetName, ILogger<TuffCardsMarkdownParser> logger) {
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

public static class MarkdownPipelineExtension {
	public static MarkdownPipelineBuilder UseImages(this MarkdownPipelineBuilder builder, ImageOptions options, ILogger logger) {
		if (!builder.Extensions.Contains<ImageExtension>()) {
			builder.Extensions.Add(new ImageExtension(options, logger));
		}
		return builder;
	}
}

public class ImageParser : InlineParser {
	public ImageParser() {
		OpeningCharacters = new[] { '{' };
	}

	public override bool Match(InlineProcessor processor, ref StringSlice slice) {
		var isImage = false;

		slice.NextChar();
		if (slice.CurrentChar == '{') {
			isImage = true;
			slice.NextChar();
		}

		var current = slice.CurrentChar;
		var start = slice.Start;
		var end = start;

		while (current.IsAlphaNumeric() || current == '-' || current == '_')
		{
			end = slice.Start;
			current = slice.NextChar();
		}
		if (slice.CurrentChar != '}') {
			return false;
		}
		slice.NextChar();
		if (isImage) {
			if (slice.CurrentChar != '}') {
				return false;
			}
			slice.NextChar();
		}

		var inlineStart = processor.GetSourcePosition
			(slice.Start, out var line, out var column);
		var result = new StringSlice(slice.Text, start, end).ToString();
		processor.Inline = new Image
		{
			Span =
			{
				Start = inlineStart,
				End = inlineStart + (end - start) + 1
			},
			Line = line,
			Column = column,
			Name = result,
			IsIcon = !isImage
		};

		return true;
	}
}

public class Image : LeafInline {
	public string Name { get; init; } = string.Empty;
	public bool IsIcon { get; init; }
}

public class ImageOptions {
	public string ImagesPath { get; init; } = string.Empty;
	public string OutputPath { get; init; } = string.Empty;
	public string IconsPath { get; init; } = string.Empty;
}

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

public class ImageRenderer : HtmlObjectRenderer<Image> {
	private readonly ImageOptions Options;
	private readonly ILogger Logger;

	public ImageRenderer(ImageOptions options, ILogger logger) {
		Options = options;
		Logger = logger;
	}

	protected override void Write(HtmlRenderer renderer, Image obj) {
		var dir = new DirectoryInfo(obj.IsIcon ? Options.IconsPath : Options.ImagesPath);
		FileInfo? file = null;
		if (dir.Exists) {
			file = dir.EnumerateFiles($"{obj.Name}.*", SearchOption.AllDirectories).FirstOrDefault();
		}
		var cssClass = obj.IsIcon ? "icon" : "image";
		if (file == null) {
			Logger.LogWarning("{cssClass} '{objName}' not found", cssClass, obj.Name);
			renderer.Write($"<em class=\"{cssClass}\">{obj.Name}</em>");
		}
		else {
			if (file.Extension == ".svg") {
				var svg = File.ReadAllText(file.FullName).AsSpan();
				renderer.Write(svg[..5]);
				renderer.Write($"class=\"{cssClass}\" ");
				renderer.Write(svg[5..]);
			}
			else {
				var fileName = $"{cssClass}-{file.Name}";
				var outputPath = Path.Combine(Options.OutputPath, fileName);
				if (!File.Exists(outputPath)) File.Copy(file.FullName, outputPath);
				renderer.Write($"<img class=\"{cssClass}\" src=\"{fileName}\" alt=\"{obj.Name}\" />");
			}
		}
	}
}