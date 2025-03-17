using System.Reflection;

namespace tuffCards.Tests.Unit;

public class ReaderTests(OdsReader OdsReader, CsvReader CsvReader, XlsxReader XlsxReader) {
	[Fact]
	[UsedImplicitly]
	public void ReadExampleCsv() {
		var path = GetPath("csv");
		if (!File.Exists(path))
			throw new FileNotFoundException(path);

		var rows = CsvReader.GetData(path);
		TestData(rows);
	}

	[Fact]
	[UsedImplicitly]
	public void ReadExampleOds() {
		var path = GetPath("ods");
		if (!File.Exists(path))
			throw new FileNotFoundException(path);

		var doc = OdsReader.GetDocument(path);
		Assert.Single(doc.GetAllSheets());

		var rows = doc.GetSheet("Sheet1")!.GetContentAsDictionary().ToList();
		TestData(rows);
	}

	[Fact]
	[UsedImplicitly]
	public void ReadExampleXlsx() {
		var path = GetPath("xlsx");
		if (!File.Exists(path))
			throw new FileNotFoundException(path);

		var doc = XlsxReader.GetDocument(path);
		Assert.Single(doc.GetAllSheets());

		var rows = doc.GetSheet("Sheet1")!.GetContentAsDictionary().ToList();
		TestData(rows);
	}

	private static string GetPath(string extension) {
		return Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + $@"\Unit\Test.{extension}");
	}

	private static void TestData(List<(string name, Dictionary<string, string> data)> rows) {
		Assert.Equal(4, rows.Count);
		Assert.Equal("1", rows[0].name);
		Assert.Equal(new Dictionary<string, string> {
			{ "Column A", "1" },
			{ "Column B", "3" },
		}, rows[0].data);
		Assert.Equal("1", rows[1].name);
		Assert.Equal(new Dictionary<string, string> {
			{ "Column A", "1" },
			{ "Column B", "3" },
		}, rows[1].data);
		Assert.Equal("2", rows[2].name);
		Assert.Equal(new Dictionary<string, string> {
			{ "Column A", "2" },
			{ "Column B", "2" },
		}, rows[2].data);
		Assert.Equal("", rows[3].name);
		Assert.Equal(new Dictionary<string, string> {
			{ "Column A", "" },
			{ "Column B", "4" },
		}, rows[3].data);
	}
}