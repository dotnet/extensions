using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNet.DependencyInjection.Tests.Fakes
{
    public class FakeOpenGenericService<T> : IFakeOpenGenericService<T>
    {
        private readonly T _otherService;

        public FakeOpenGenericService(T otherService)
        {
            _otherService = otherService;
        }

        public T SimpleMethod()
        {
            return _otherService;
        }
    }
}
