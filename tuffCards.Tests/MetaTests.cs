using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using tuffCards.Tests.Helpers;
using tuffLib.Functional;

namespace tuffCards.Tests;

public class MetaTests(IServiceProvider provider, ITestOutputHelper testOutputHelper) {

	[Fact]
	public void ShouldResolveLoggerFactory() {
		var loggerFactory = provider.GetService<ILoggerFactory>();
		Assert.NotNull(loggerFactory);
	}

	[Fact]
	public void ShouldResolveTemporaryDirectoryFactory() {
		var temporaryDirectoryFactory = provider.GetService<TemporaryDirectoryFactory>();
		Assert.NotNull(temporaryDirectoryFactory);
	}

	[Fact]
	public void ShouldResolveAllServices() {
		var allServices = provider.GetServices<object>();

		foreach (var service in allServices) {
			service.GetType().FullName?.Apply(testOutputHelper.WriteLine);
		}
	}
}