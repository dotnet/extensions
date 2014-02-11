using Microsoft.AspNet.DependencyInjection.Tests.Fakes;
using Xunit;

namespace Microsoft.AspNet.DependencyInjection.Tests
{
    public class TypeActivatorTests
    {
        [Fact]
        public void TypeActivatorEnablesYouToCreateAnyTypeWithServicesEvenWhenNotInIocContainer()
        {
            var serviceProvider = new ServiceProvider()
                .Add<IFakeService, FakeService>()
                .Add<ITypeActivator, TypeActivator>();

            var typeActivator = serviceProvider.GetService<ITypeActivator>();

            var anotherClass = typeActivator.CreateInstance<AnotherClass>();

            var result = anotherClass.LessSimpleMethod();

            Assert.Equal("[FakeServiceSimpleMethod]", result);
        }
    }
}
