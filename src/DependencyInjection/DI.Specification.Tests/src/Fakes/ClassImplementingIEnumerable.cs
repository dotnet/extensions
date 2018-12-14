using System;
using System.Collections;

namespace Microsoft.Extensions.DependencyInjection.Specification.Fakes
{
    public class ClassImplementingIEnumerable : IEnumerable
    {
        public IEnumerator GetEnumerator() => throw new NotImplementedException();
    }
}
