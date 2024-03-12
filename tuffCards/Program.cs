using System.CommandLine;
using System.Threading.Tasks;

namespace TuffCards;

internal static class Program {
	public static async Task<int> Main(string[] args) {
		var root = new RootCommand("tuffCards is a small app to convert html/css/csv templates to cards.");

		var wrapperArg = new Option<string>("--wrapper", () => "default", "The name of the wrapper file (in /wrappers).");
		var forceArg = new Option<bool>("--force", () => false, "Force the command, ignore warnings.");
		var screenshotArg = new Option<bool>("--screenshot", () => false, "Take a screenshot (with chrome expected at default path). You should specify the size in the wrapper.");

		var convertCmd = new Command("convert", "Converts the cards to html pages.") {
			wrapperArg,
			screenshotArg
		};
		root.AddCommand(convertCmd);
		convertCmd.SetHandler(async (wrapper, screenshot) => await Converter.Convert(wrapper, screenshot), wrapperArg, screenshotArg);

		var createCmd = new Command("create", "Creates a new project in this folder.") {
			forceArg
		};
		root.AddCommand(createCmd);
		createCmd.SetHandler(async (force) => await Creator.Create(force), forceArg);

		return await root.InvokeAsync(args);
	}
}