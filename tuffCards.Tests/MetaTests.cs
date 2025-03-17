namespace tuffCards.Tests;

public class MetaTests(IServiceProvider Provider, ITestOutputHelper TestOutputHelper) {

	[Fact]
	[UsedImplicitly]
	public void ShouldResolveLoggerFactory() {
		var loggerFactory = Provider.GetService<ILoggerFactory>();
		Assert.NotNull(loggerFactory);
	}

	[Fact]
	[UsedImplicitly]
	public void ShouldResolveTemporaryDirectoryFactory() {
		var temporaryDirectoryFactory = Provider.GetService<TemporaryDirectoryFactory>();
		Assert.NotNull(temporaryDirectoryFactory);
	}

	[Fact]
	[UsedImplicitly]
	public void ShouldResolveAllServices() {
		var allServices = Provider.GetServices<object>();

		foreach (var service in allServices) {
			service.GetType().FullName?.Apply(TestOutputHelper.WriteLine);
		}
	}
}