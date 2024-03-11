using System.CommandLine;
using System.Threading.Tasks;

namespace TuffCards;

internal static class Program {
	public static async Task<int> Main(string[] args) {
		var root = new RootCommand("tuffCards");

		var wrapperArg = new Option<string>("--wrapper", () => "default.html", "The name of the wrapper file (in /wrappers).");
		var forceArg = new Option<bool>("--force", () => false, "Force the command, ignore warnings.");

		var convertCmd = new Command("convert", "Converts the cards to html pages.") {
			wrapperArg
		};
		root.AddCommand(convertCmd);
		convertCmd.SetHandler(async (wrapper) => await Converter.Convert(wrapper), wrapperArg);

		var createCmd = new Command("create", "Creates a new project in this folder.") {
			forceArg
		};
		root.AddCommand(createCmd);
		createCmd.SetHandler(async (force) => await Creator.Create(force), forceArg);

		return await root.InvokeAsync(args);
	}
}