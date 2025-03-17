namespace tuffCards.Presets;

public static class Defaults {
	private static string GetResource(string resourceName) {
	   var assembly = Assembly.GetExecutingAssembly();
	   using var stream = assembly.GetManifestResourceStream($"tuffCards.Presets.{resourceName}");
	   if (stream == null)
		   throw new FileNotFoundException($"Resource '{resourceName}' not found. Resources are: {string.Join(", ", assembly.GetManifestResourceNames())}");

	   using var reader = new StreamReader(stream);
	   return reader.ReadToEnd();
	}

	public static string Overview => GetResource("Overview.html");

	public static string GlobalTargetCss => GetResource("GlobalTarget.css");

	public static string DefaultTarget => GetResource("DefaultTarget.html");
	public static string SpriteTarget => GetResource("SpriteTarget.html");

	public static string ExampleActions => GetResource("ExampleActions.html");
	public static string ExampleActionsData => GetResource("ExampleActions.csv");
	public static string ExampleActionsCss => GetResource("ExampleActions.css");

	public static string ExampleBuildings => GetResource("ExampleBuildings.html");
	public static string ExampleBuildingsData => GetResource("ExampleBuildings.csv");
	public static string ExampleBuildingsCss => GetResource("ExampleBuildings.css");

	public static string FitTextScript => GetResource("FitText.js");

	public static string TapImage => GetResource("Tap.svg");
	public static string StrongImage => GetResource("Strong.svg");
}