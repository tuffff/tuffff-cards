using nietras.SeparatedValues;

namespace tuffCards.Services;

public class CsvReader(ILogger<CsvReader> Logger) {
	public List<(string name, Dictionary<string, string> data)> GetData(string cardDataPath) {
		try {
			using var reader = Sep
				.Reader(o => o with { Unescape = true })
				.FromFile(cardDataPath);
			return reader
				.Enumerate(row => {
					if (row.Span.Length == 0 || row.Span.StartsWith("//"))
						return ("", new());
					var title = row[0].ToString();
					var dict = new Dictionary<string, string>();
					foreach (var header in reader.Header.ColNames)
						dict[header] = row[header].ToString();
					return (title, dict);
				})
				.Where(d => d.dict.Count != 0)
				.ToList();
		}
		catch (Exception ex) {
			Logger.LogError("Parsing card data: {message}. Skipping.", ex.Message);
			return [];
		}
	}
}