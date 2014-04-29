using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.DependencyInjection.Tests.Fakes;
using Xunit;

namespace Microsoft.AspNet.DependencyInjection.Tests
{
    public abstract class AllContainerTestsBase
    {
        protected abstract IServiceProvider CreateContainer();

        [Fact]
        public void SingleServiceCanBeResolved()
        {
            var container = CreateContainer();

            var service = container.GetService<IFakeService>();

            Assert.NotNull(service);
            Assert.Equal("FakeServiceSimpleMethod", service.SimpleMethod());
        }

        public void ServiceInstanceCanBeResolved()
        {
            var container = CreateContainer();

            var service = container.GetService<IFakeServiceInstance>();

            Assert.NotNull(service);
            Assert.Equal("Instance", service.SimpleMethod());
        }

        [Fact]
        public void TransientServiceCanBeResolved()
        {
            var container = CreateContainer();

            var service1 = container.GetService<IFakeService>();
            var service2 = container.GetService<IFakeService>();

            Assert.NotNull(service1);
            Assert.NotEqual(service1, service2);
        }

        [Fact]
        public void SingleServiceCanBeIEnumerableResolved()
        {
            var container = CreateContainer();

            var services = container.GetService<IEnumerable<IFakeService>>();

            Assert.NotNull(services);
            Assert.Equal(1, services.Count());
            Assert.Equal("FakeServiceSimpleMethod", services.Single().SimpleMethod());
        }

        [Fact]
        public void MultipleServiceCanBeIEnumerableResolved()
        {
            var container = CreateContainer();

            var services = container.GetService<IEnumerable<IFakeMultipleService>>();

            var results = services.Select(x => x.SimpleMethod()).ToArray();

            Assert.NotNull(results);
            Assert.Equal(2, results.Count());
            Assert.Contains("FakeOneMultipleServiceAnotherMethod", results);
            Assert.Contains("FakeTwoMultipleServiceAnotherMethod", results);
        }

        [Fact]
        public void OuterServiceCanHaveOtherServicesInjected()
        {
            var container = CreateContainer();

            var service = container.GetService<IFakeOuterService>();

            string singleValue;
            string[] multipleValues;
            service.Interrogate(out singleValue, out multipleValues);

            Assert.NotNull(service);
            Assert.Equal(2, multipleValues.Count());
            Assert.Contains("FakeServiceSimpleMethod", singleValue);
            Assert.Contains("FakeOneMultipleServiceAnotherMethod", multipleValues);
            Assert.Contains("FakeTwoMultipleServiceAnotherMethod", multipleValues);
        }

        [Fact]
        public void SetupCallsSortedInOrder()
        {
            var container = CreateContainer();
            var service = container.GetService<IOptionsAccessor<FakeOptions>>();

            Assert.NotNull(service);
            var options = service.Options;
            Assert.NotNull(options);
            Assert.Equal("ABC", options.Message);
        }

    }
}
