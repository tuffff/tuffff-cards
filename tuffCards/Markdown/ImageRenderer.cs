namespace tuffCards.Markdown;

internal class ImageRenderer : HtmlObjectRenderer<Image> {
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