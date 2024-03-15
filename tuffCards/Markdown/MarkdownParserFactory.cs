namespace tuffCards.Markdown;

public class MarkdownParserFactory {
	private readonly FolderRepository FolderRepository;
	private readonly ILogger<TuffCardsMarkdownParser> Logger;

	public MarkdownParserFactory(FolderRepository folderRepository, ILogger<TuffCardsMarkdownParser> logger) {
		FolderRepository = folderRepository;
		Logger = logger;
	}

	public TuffCardsMarkdownParser Build(string targetName) {
		return new TuffCardsMarkdownParser(FolderRepository, targetName, Logger);
	}
}