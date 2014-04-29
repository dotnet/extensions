using System;
using System.Linq;
using System.Collections.Generic;

namespace Microsoft.AspNet.DependencyInjection
{
    public interface IOptionsAccessor<out TOptions> where TOptions : new()
    {
        TOptions Options { get; }
    }
}