using System.Text.RegularExpressions;

namespace tuffCards.Extensions;

public static partial class StringToPixelsExtension {
	public static int ToPixels(this string? bleed) {
		if (bleed == null)
			return 0;
		var match = LengthRegex().Match(bleed);
		if (!match.Success)
			throw new Exception("Invalid bleed value");
		var bleedValue = int.Parse(match.Groups[1].Value);
		if (bleedValue < 0)
			throw new Exception("Bleed value cannot be < 0");
		var bleedUnit = match.Groups[2].Value;
		var bleedPixels = Convert.ToInt32(bleedUnit switch {
			"px" => bleedValue,
			"mm" => bleedValue * 3.7795275591,
			"cm" => bleedValue * 37.795275591,
			"in" => bleedValue * 90,
			"pt" => bleedValue * 1.3333333333,
			"pc" => bleedValue * 16,
			_ => throw new Exception($"Unknown bleed unit: {bleedUnit}")
		});
		return bleedPixels;
	}

    [GeneratedRegex(@"(\d+)\s*(\D+)")]
    private static partial Regex LengthRegex();
}