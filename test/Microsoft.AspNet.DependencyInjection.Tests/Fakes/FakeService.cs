using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNet.DependencyInjection.Tests.Fakes
{
    public class FakeService : IFakeService
    {
        public string SimpleMethod()
        {
            return "FakeServiceSimpleMethod";
        }
    }
}
