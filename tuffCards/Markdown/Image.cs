namespace tuffCards.Markdown;

public class Image : LeafInline {
	public string Name { get; init; } = string.Empty;
	public bool IsIcon { get; init; }
}