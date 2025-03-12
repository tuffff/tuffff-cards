using System.Diagnostics.CodeAnalysis;

namespace tuffCards.Commands.ConverterModels;

[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
public class DeckModel(CustomMarkdownParser Parser) {
	public string name { get; set; } = "";
	public IList<string> cards { get; set; } = new List<string>();
	public string cardtypecss { get; set; } = "";
	public string globaltargetcss { get; set; } = "";
	public IList<string> scripts { get; set; } = new List<string>();
	public string md(string input) => Parser.Parse(input);
}