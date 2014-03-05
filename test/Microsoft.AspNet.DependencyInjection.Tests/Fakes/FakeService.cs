using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNet.DependencyInjection.Tests.Fakes
{
    public class FakeService : IFakeService, IFakeServiceInstance, IFakeSingletonService
    {
        public FakeService()
        {
            Message = "FakeServiceSimpleMethod";
        }

        public string Message { get; set; }

        public string SimpleMethod()
        {
            return Message;
        }
    }
}
