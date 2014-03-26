using Microsoft.AspNet.DependencyInjection.Fallback;
using Microsoft.AspNet.DependencyInjection.Tests.Fakes;
using Xunit;

namespace Microsoft.AspNet.DependencyInjection.Tests
{
    public class TypeActivatorTests
    {
        [Fact]
        public void TypeActivatorEnablesYouToCreateAnyTypeWithServicesEvenWhenNotInIocContainer()
        {
            var serviceProvider = new ServiceCollection()
                .AddTransient<IFakeService, FakeService>()
                .AddTransient<ITypeActivator, TypeActivator>()
                .BuildServiceProvider();

            var typeActivator = serviceProvider.GetService<ITypeActivator>();

            var anotherClass = typeActivator.CreateInstance<AnotherClass>(serviceProvider);

            var result = anotherClass.LessSimpleMethod();

            Assert.Equal("[FakeServiceSimpleMethod]", result);
        }

        [Fact]
        public void TypeActivatorAcceptsAnyNumberOfAdditionalConstructorParametersToProvide()
        {
            var serviceProvider = new ServiceCollection()
                .AddTransient<IFakeService, FakeService>()
                .AddTransient<ITypeActivator, TypeActivator>()
                .BuildServiceProvider();

            var typeActivator = serviceProvider.GetService<ITypeActivator>();

            var anotherClass = typeActivator.CreateInstance<AnotherClassAcceptingData>(serviceProvider, "1", "2");

            var result = anotherClass.LessSimpleMethod();

            Assert.Equal("[FakeServiceSimpleMethod] 1 2", result);
        }

        [Fact]
        public void TypeActivatorWorksWithStaticCtor()
        {
            var serviceProvider = new ServiceCollection()
                .AddTransient<ITypeActivator, TypeActivator>()
                .BuildServiceProvider();

            var typeActivator = serviceProvider.GetService<ITypeActivator>();

            var anotherClass = typeActivator.CreateInstance<ClassWithStaticCtor>(serviceProvider);

            Assert.NotNull(anotherClass);
        }
    }
}
