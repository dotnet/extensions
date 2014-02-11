using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNet.DependencyInjection.Tests.Fakes
{
    public class AnotherClass
    {
        private readonly IFakeService _fakeService;

        public AnotherClass(IFakeService fakeService)
        {
            _fakeService = fakeService;
        }

        public string LessSimpleMethod()
        {
            return "[" + _fakeService.SimpleMethod() + "]";
        }
    }
}
