using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using tuffCards.Tests.Helpers;
using tuffLib.Functional;

namespace tuffCards.Tests;

public class MetaTests(IServiceProvider Provider, ITestOutputHelper TestOutputHelper) {

	[Fact]
	public void ShouldResolveLoggerFactory() {
		var loggerFactory = Provider.GetService<ILoggerFactory>();
		Assert.NotNull(loggerFactory);
	}

	[Fact]
	public void ShouldResolveTemporaryDirectoryFactory() {
		var temporaryDirectoryFactory = Provider.GetService<TemporaryDirectoryFactory>();
		Assert.NotNull(temporaryDirectoryFactory);
	}

	[Fact]
	public void ShouldResolveAllServices() {
		var allServices = Provider.GetServices<object>();

		foreach (var service in allServices) {
			service.GetType().FullName?.Apply(TestOutputHelper.WriteLine);
		}
	}
}