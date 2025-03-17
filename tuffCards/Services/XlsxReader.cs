using ClosedXML.Excel;

namespace tuffCards.Services;

public class XlsxReader : TableReader, IDisposable {
	protected override ITableDocument LoadDocument(string path) {
		if (!File.Exists(path))
			throw new FileNotFoundException($"The file at '{path}' does not exist.");

		return new XlsxDocument(path);
	}

	public void Dispose() {
		foreach (var document in DocumentCache.Values.OfType<XlsxDocument>())
			document.Dispose();
		GC.SuppressFinalize(this);
	}
}

public class XlsxDocument(string Path) : ITableDocument, IDisposable {
	private readonly XLWorkbook Document = new(Path);

	public IEnumerable<ITableSheet> GetAllSheets() {
		return Document.Worksheets.Select(sheet => new XlsxSheet(sheet));
	}

	public ITableSheet? GetSheet(string name) {
		try {
			return new XlsxSheet(Document.Worksheet(name));
		}
		catch {
			return null;
		}
	}

	public void Dispose() {
		Document.Dispose();
		GC.SuppressFinalize(this);
	}
}

public class XlsxSheet(IXLWorksheet Sheet) : ITableSheet {
	public IEnumerable<List<string>> GetRows() {
		return Sheet
			.RangeUsed()?
			.RowsUsed()
			.Select(r =>
				r.Cells(1, r.CellsUsed().Last().Address.ColumnNumber)
					.Select(c => c.Value.ToString() ?? string.Empty)
					.ToList())
			?? [];
	}

	public IEnumerable<(string name, Dictionary<string, string> data)> GetContentAsDictionary() {
		var rows = GetRows().ToList();
		return rows
			.Skip(1)
			.Select(row => (
					row.FirstOrDefault() ?? string.Empty,
					row.ZipLongest(rows[0], (value, header) => (value, header))
						.Where(t => t.header != null)
						.ToDictionary(t => t.header!, t => t.value ?? string.Empty)
				)
			);
	}
}