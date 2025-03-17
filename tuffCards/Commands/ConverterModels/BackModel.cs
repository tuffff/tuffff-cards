namespace tuffCards.Commands.ConverterModels;

[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
public class BackModel(CustomMarkdownParser Parser) {
	public string deck { get; set; } = "";
	public string md(string input) => Parser.Parse(input);
}