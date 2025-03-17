namespace tuffCards.Services;

public abstract class TableReader {
	protected readonly Dictionary<string, ITableDocument> DocumentCache = new();

	public ITableDocument GetDocument(string path) {
		if (DocumentCache.TryGetValue(path, out var doc)) {
			return doc;
		}
		var newDoc = LoadDocument(path);
		DocumentCache[path] = newDoc;
		return newDoc;
	}

	public void ClearCache() {
		DocumentCache.Clear();
	}

	protected abstract ITableDocument LoadDocument(string path);
}

public interface ITableDocument {
	IEnumerable<ITableSheet> GetAllSheets();
	ITableSheet? GetSheet(string name);
}

public interface ITableSheet {
	IEnumerable<List<string>> GetRows();
	IEnumerable<(string name, Dictionary<string, string> data)> GetContentAsDictionary();
}