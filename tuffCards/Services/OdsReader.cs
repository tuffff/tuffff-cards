using MoreLinq.Extensions;
using System.IO.Compression;
using System.Xml;
using tuffLib.Functional;

namespace tuffCards.Services;

public class OdsReader {
	private readonly Dictionary<string, OdsDocument> DocumentCache = new();

	public OdsDocument GetDocument(string path) {
		if (DocumentCache.TryGetValue(path, out var doc)) {
			return doc;
		}
		var newDoc = LoadDocument(path);
		DocumentCache[path] = newDoc;
		return newDoc;
	}

	private static OdsDocument LoadDocument(string path) {
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

	public void ClearCache() {
		DocumentCache.Clear();
	}
}

public class OdsDocument {
	private readonly Dictionary<string, XmlNode> Sheets;

	public OdsDocument(XmlDocument document) {
		Sheets = document
			.GetElementsByTagName("table:table")
			.OfType<XmlNode>()
			.Select(node => (node, name: node.Attributes?["table:name"]?.Value))
			.Where(t => t.name != null)
			.ToDictionary(t => t.name!, t => t.node);
	}

	public List<string> GetSheetNames() {
		return Sheets.Keys.ToList();
	}

	public List<OdsSheet> GetAllSheets() {
		return Sheets.Values.Select(s => new OdsSheet(s)).ToList();
	}

	public OdsSheet? GetSheet(string name) {
		return Sheets.GetValueOrDefault(name)?.Apply(s => new OdsSheet(s));
	}
}

public class OdsSheet {
	private readonly List<List<string>> Rows;

	public OdsSheet(XmlNode sheet) {
		Rows = sheet.ChildNodes
			.OfType<XmlNode>()
			.Where(row => row.Name == "table:table-row")
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

	public List<List<string>> GetRows() {
		return Rows;
	}

	public List<(string name, Dictionary<string, string> data)> GetContentAsDictionary() {
		return Rows
			.Skip(1)
			.Select(row => (
				row.FirstOrDefault() ?? string.Empty,
				row
					.ZipLongest(Rows[0], (value, header) => (value, header))
					.Where(t => t.header != null)
					.ToDictionary(t => t.header!, t => t.value ?? string.Empty)
				)
			)
			.ToList();
	}
}