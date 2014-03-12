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

        public object GetService(Type serviceType)
        {
            _timesGetServiceHasBeenInvoked++;

            if (serviceType == typeof(int))
            {
                return _timesGetServiceHasBeenInvoked;
            }
            else if (serviceType == typeof(string))
            {
                return "FakeFallbackServiceProvider";
            }
            else if (serviceType == typeof(IFakeFallbackService))
            {
                return new FakeService()
                {
                    Message = "FakeFallbackServiceProvider"
                };
            }
            else
            {
                return null;
            }
        }
    }
}
