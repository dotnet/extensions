using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNet.DependencyInjection.Tests.Fakes
{
    interface IFakeEveryService :
            IFakeService,
            IFakeMultipleService,
            IFakeScopedService,
            IFakeServiceInstance,
            IFakeSingletonService
    {
    }
}
