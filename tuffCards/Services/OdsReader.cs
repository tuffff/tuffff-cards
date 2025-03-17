using System.IO.Compression;
using System.Xml;

namespace tuffCards.Services;

public class OdsReader : TableReader {
	protected override ITableDocument LoadDocument(string path) {
		if (!File.Exists(path))
			throw new FileNotFoundException($"The file at '{path}' does not exist.");

		using var archive = ZipFile.OpenRead(path);
		var contentEntry = archive.GetEntry("content.xml")
			?? throw new InvalidDataException("The file does not contain a content.xml entry.");

		using var contentStream = contentEntry.Open();
		var document = new XmlDocument();
		document.Load(contentStream);
		return new OdsDocument(document);
	}
}

public class OdsDocument : ITableDocument {
	private readonly Dictionary<string, XmlNode> Sheets;

	public OdsDocument(XmlDocument document) {
		Sheets = document
			.GetElementsByTagName("table:table")
			.OfType<XmlNode>()
			.Select(node => (node, name: node.Attributes?["table:name"]?.Value))
			.Where(t => t.name != null)
			.ToDictionary(t => t.name!, t => t.node);
	}

	public IEnumerable<string> GetSheetNames() {
		return Sheets.Keys;
	}

	public IEnumerable<ITableSheet> GetAllSheets() {
		return Sheets.Values.Select(s => new OdsSheet(s)).ToList();
	}

	public ITableSheet? GetSheet(string name) {
		return Sheets.GetValueOrDefault(name)?.Apply(s => new OdsSheet(s));
	}
}

public class OdsSheet : ITableSheet {
	private readonly List<List<string>> Rows;

	public OdsSheet(XmlNode sheet) {
		Rows = sheet.ChildNodes
			.OfType<XmlNode>()
			.Where(row => row.Name == "table:table-row")
			.SelectMany(row => int.TryParse(row.Attributes?["table:number-rows-repeated"]?.Value, out var count)
				? Enumerable.Repeat(row, count)
				: [row])
			.Select(row => row.ChildNodes.OfType<XmlNode>().Where(cell => cell.Name == "table:table-cell"))
			.Where(row => row.Any())
			.Select(row => row
				.SelectMany(cell => int.TryParse(cell.Attributes?["table:number-columns-repeated"]?.Value, out var count)
					? Enumerable.Repeat(cell, count)
					: [cell])
				.Select(cell => cell.Attributes?["office:value-type"]?.Value switch {
					"float" => cell.Attributes?["office:value"]?.Value ?? string.Empty,
					null => string.Empty,
					_ => cell.InnerText
				})
				.ToList())
			.ToList();
	}

	public IEnumerable<List<string>> GetRows() {
		return Rows;
	}

	public IEnumerable<(string name, Dictionary<string, string> data)> GetContentAsDictionary() {
		return Rows
			.Skip(1)
			.Select(row => (
					row.FirstOrDefault() ?? string.Empty,
					row.ZipLongest(Rows[0], (value, header) => (value, header))
						.Where(t => t.header != null)
						.ToDictionary(t => t.header!, t => t.value ?? string.Empty)
				)
			);
	}
}