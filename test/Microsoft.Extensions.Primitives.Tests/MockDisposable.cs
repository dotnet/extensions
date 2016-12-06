using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Primitives.Tests
{
    public class MockDisposable: IDisposable
    {
        public bool Disposed { get; set; }

        public void Dispose()
        {
            Disposed = true;
        }
    }
}
