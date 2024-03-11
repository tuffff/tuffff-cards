using Markdig;
using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax.Inlines;
using System;
using System.IO;
using System.Linq;

namespace TuffCards;

public class MarkdownParser {
	private readonly MarkdownPipeline Pipeline;

	public MarkdownParser(string iconsPath, string imagesPath, string outputPath) {
		Pipeline = new MarkdownPipelineBuilder()
			.UseImages(new ImageOptions {
				ImagesPath = imagesPath,
				IconsPath = iconsPath,
				OutputPath = outputPath
			})
			.Build();
	}

	public string Parse(string md) {
		return Markdown.ToHtml(md, Pipeline);
	}

}

public static class MarkdownPipelineExtension {
	public static MarkdownPipelineBuilder UseImages(this MarkdownPipelineBuilder builder, ImageOptions options) {
		if (!builder.Extensions.Contains<ImageExtension>()) {
			builder.Extensions.Add(new ImageExtension(options));
		}
		return builder;
	}
}

public class ImageParser : InlineParser {
	public ImageParser() {
		OpeningCharacters = new[] { '\'', '"' };
	}

	public override bool Match(InlineProcessor processor, ref StringSlice slice) {
		var opening = slice.CurrentChar;
		slice.NextChar();

		var current = slice.CurrentChar;
		var start = slice.Start;
		var end = start;

		while (current.IsAlpha())
		{
			end = slice.Start;
			current = slice.NextChar();
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
			IsIcon = opening == '\''
		};

		return true;
	}
}

public class Image : LeafInline {
	public string Name { get; set; } = string.Empty;
	public bool IsIcon { get; set; }
}

public class ImageOptions {
	public string ImagesPath { get; set; } = string.Empty;
	public string OutputPath { get; set; } = string.Empty;
	public string IconsPath { get; set; } = string.Empty;
}

public class ImageExtension : IMarkdownExtension {
	private readonly ImageOptions Options;
	public ImageExtension(ImageOptions options) {
		Options = options;
	}

	public void Setup(MarkdownPipelineBuilder pipeline) {
		pipeline.InlineParsers.Add(new ImageParser());
	}

	public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer) {
		renderer.ObjectRenderers.Add(new ImageRenderer(Options));
	}
}

public class ImageRenderer : HtmlObjectRenderer<Image> {
	private readonly ImageOptions Options;

	public ImageRenderer(ImageOptions options) {
		Options = options;
	}

	protected override void Write(HtmlRenderer renderer, Image obj) {
		var dir = new DirectoryInfo(obj.IsIcon ? Options.IconsPath : Options.ImagesPath);
		FileInfo? file = null;
		if (dir.Exists) {
			file = dir.EnumerateFiles($"{obj.Name}.*", SearchOption.AllDirectories).FirstOrDefault();
		}
		if (file == null) {
			Console.WriteLine($"Warning: Image '{obj.Name}' not found");
			renderer.Write($"<em>{obj.Name}</em>");
		}
		else {
			var cssClass = obj.IsIcon ? "icon" : "image";
			var fileName = $"{cssClass}-{file.Name}";
			var outputPath = Path.Combine(Options.OutputPath, fileName);
			if (!File.Exists(outputPath)) File.Copy(file.FullName, outputPath);
			renderer.Write($"<img class=\"{cssClass}\" src=\"{fileName}\" alt=\"{obj.Name}\" />");
		}
	}
}