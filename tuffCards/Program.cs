using System.CommandLine;
using System.Threading.Tasks;

namespace TuffCards;

internal static class Program {
	public static async Task<int> Main(string[] args) {
		var root = new RootCommand("tuffCards is a small tool to convert html/css/csv templates to cards.");

		var targetArg = new Option<string>("--target", () => "default", "The name of the target file (in /targets).");
		var forceArg = new Option<bool>("--force", () => false, "Force the command, ignore warnings.");
		var generateImageArg = new Option<bool>("--image", () => false, "Creates a png by taking a render after generating the html (with chrome expected at default path). You should specify the size in the target.");
		var cardTypeNameArg = new Argument<string>("name", "The name of the card type (also the file name).");

		var convertCmd = new Command("convert", "Converts the cards to html pages.") {
			targetArg,
			generateImageArg
		};
		root.AddCommand(convertCmd);
		convertCmd.SetHandler(async (target, image) => await Converter.Convert(target, image), targetArg, generateImageArg);

		var createCmd = new Command("create", "Creates a new, relatively empty project in this folder.") {
			forceArg
		};
		root.AddCommand(createCmd);
		createCmd.SetHandler(async (force) => await Creator.Create(force), forceArg);

		var createExampleCmd = new Command("create-example", "Creates a new project with example cards in this folder.") {
			forceArg
		};
		root.AddCommand(createExampleCmd);
		createExampleCmd.SetHandler(async (force) => await Creator.CreateExample(force), forceArg);

		var addCardType = new Command("add-type", "Adds a new card type to the project. A .html, .csv and .css file is created.") {
			cardTypeNameArg,
			forceArg
		};
		root.AddCommand(addCardType);
		addCardType.SetHandler(async (name, force) => await CardTypeAdder.Add(name, force), cardTypeNameArg, forceArg);

		return await root.InvokeAsync(args);
	}
}