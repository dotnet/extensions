using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.Logging.Testing
{
    public interface ITestSink
    {
        Func<WriteContext, bool> WriteEnabled { get; set; }

        Func<BeginScopeContext, bool> BeginEnabled { get; set; }

        List<BeginScopeContext> Scopes { get; set; }

        List<WriteContext> Writes { get; set; }

        void Write(WriteContext context);

        void Begin(BeginScopeContext context);
    }
}
