using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNet.DependencyInjection.Tests.Fakes
{
    public class FakeFallbackServiceProvider : IServiceProvider
    {
        private int _timesGetServiceHasBeenInvoked = 0;
        private string _prefix;

        public FakeFallbackServiceProvider()
            : this(prefix: "")
        {
        }

        public FakeFallbackServiceProvider(string prefix)
        {
            _prefix = prefix;
        }

        public object GetService(Type serviceType)
        {
            _timesGetServiceHasBeenInvoked++;

            if (serviceType == typeof(int))
            {
                return _timesGetServiceHasBeenInvoked;
            }
            else if (serviceType == typeof(string))
            {
                return _prefix + "FakeFallbackServiceProvider";
            }
            else if (serviceType == typeof(IEnumerable<string>))
            {
                return new[] { _prefix + "FakeFallbackServiceProvider" };
            }
            else if (serviceType == typeof(IFakeFallbackService))
            {
                return new FakeService()
                {
                    Message = _prefix + "FakeFallbackServiceProvider"
                };
            }
            else if (serviceType == typeof(IServiceScopeFactory))
            {
                return new FakeFallbackScopeFactory(_prefix);
            }
            else
            {
                throw new Exception();
            }
        }

        private class FakeFallbackScopeFactory : IServiceScopeFactory
        {
            private string _prefix;

            public FakeFallbackScopeFactory(string prefix)
            {
                _prefix = prefix;
            }

            public IServiceScope CreateScope()
            {
                return new FakeFallbackScope(_prefix);
            }

            private class FakeFallbackScope : IServiceScope
            {
                public FakeFallbackScope(string prefix)
                {
                    ServiceProvider = new FakeFallbackServiceProvider("scope-" + prefix);
                }

                public IServiceProvider ServiceProvider { get; private set; }

                public void Dispose()
                {
                }
            }
        }
    }
}
