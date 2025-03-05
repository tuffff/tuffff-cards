using PuppeteerSharp;

namespace tuffCards.Services;

public sealed class BrowserService(ILogger<BrowserService> Logger) : IDisposable, IAsyncDisposable {
	private IBrowser? Browser;

	public async Task<IPage> GetPage() {
		lock (this) {
			if (Browser == null) {
				var browserFetcher = new BrowserFetcher(SupportedBrowser.Chrome);
				Logger.LogInformation("Getting browser, this make take some time ...");
				browserFetcher.DownloadAsync().Wait();
				Logger.LogDebug("Launching ...");
				Browser = Puppeteer.LaunchAsync(new LaunchOptions {
					Headless = true,
					Browser = SupportedBrowser.Chrome,
					DefaultViewport = new ViewPortOptions { Width = 1, Height = 1 }
				}).Result;
			}
		}
		Logger.LogDebug("Navigating ...");
		return await Browser.NewPageAsync();
	}

	public void Dispose() {
		Browser?.CloseAsync().Wait();
		Browser?.Dispose();
	}

	public async ValueTask DisposeAsync() {
		if (Browser != null) {
			await Browser.CloseAsync();
			await Browser.DisposeAsync();
		}
	}
}