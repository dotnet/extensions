using System;

namespace Microsoft.AspNet.DependencyInjection.Tests.Fakes
{
    public class FakeNonScopingFallbackServiceProvder : IServiceProvider
    {
        public object GetService(Type serviceType)
        {
            if (serviceType == typeof(string))
            {
                return "FakeNonScopingFallbackServiceProvder";
            }
            else
            {
                throw new Exception();
            }
        }
    }
}
