namespace tuffCards.Extensions;

public static class ScriptObjectExtensions {
	public static ScriptObject ToScriptObject(this IDictionary<string, string> obj, CustomMarkdownParser parser) {
		var scriptObject = new ScriptObject();
		scriptObject.Import(obj);
		scriptObject.Import("md", new Func<string, string>(parser.Parse));
		return scriptObject;
	}
}