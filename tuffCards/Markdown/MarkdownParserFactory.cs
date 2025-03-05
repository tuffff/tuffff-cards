using tuffCards.Services;

namespace tuffCards.Markdown;

public class MarkdownParserFactory {
	private readonly FolderRepository FolderRepository;
	private readonly ILogger<MarkdownPipeline> Logger;

	public MarkdownParserFactory(FolderRepository folderRepository, ILogger<MarkdownPipeline> logger) {
		FolderRepository = folderRepository;
		Logger = logger;
	}

	public CustomMarkdownParser Build(string targetName) {
		return new CustomMarkdownParser(FolderRepository, targetName, Logger);
	}
}