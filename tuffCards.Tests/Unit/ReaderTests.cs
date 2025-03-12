using System.Reflection;
using tuffCards.Services;

namespace tuffCards.Tests.Unit;

public class ReaderTests(OdsReader OdsReader, CsvReader CsvReader) {
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
		Assert.Equal(["Sheet1"], doc.GetSheetNames());

		var rows = doc.GetSheet("Sheet1")!.GetContentAsDictionary();
		TestData(rows);
	}

	private static string GetPath(string extension) {
		return Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + $@"\Unit\Test.{extension}");
	}

	private static void TestData(List<(string name, Dictionary<string, string> data)> rows) {
		Assert.Equal(3, rows.Count);
		Assert.Equal("1", rows[0].name);
		Assert.Equal(new Dictionary<string, string> {
			{ "Column A", "1" },
			{ "Column B", "3" },
		}, rows[0].data);
		Assert.Equal("2", rows[1].name);
		Assert.Equal(new Dictionary<string, string> {
			{ "Column A", "2" },
			{ "Column B", "2" },
		}, rows[1].data);
		Assert.Equal("", rows[2].name);
		Assert.Equal(new Dictionary<string, string> {
			{ "Column A", "" },
			{ "Column B", "4" },
		}, rows[2].data);
	}
}