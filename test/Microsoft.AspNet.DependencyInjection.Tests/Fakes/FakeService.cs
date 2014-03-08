using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNet.DependencyInjection.Tests.Fakes
{
    public class FakeService : IFakeEveryService, IDisposable
    {
        public FakeService()
        {
            Message = "FakeServiceSimpleMethod";
        }

        public bool Disposed { get; private set; }

        public string Message { get; set; }

        public string SimpleMethod()
        {
            return Message;
        }

        public void Dispose()
        {
            Disposed = true;
        }
    }
}
