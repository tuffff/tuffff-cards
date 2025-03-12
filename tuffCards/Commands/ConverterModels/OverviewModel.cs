using System.Diagnostics.CodeAnalysis;

namespace tuffCards.Commands.ConverterModels;

[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
public class OverviewModel {
	public string target { get; set; } = "";
	public IList<string> decks { get; set; } = new List<string>();
}