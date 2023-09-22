// Assembly 'Microsoft.Extensions.Hosting.Testing'

using System;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Hosting.Testing;

public class FakeHostOptions
{
    public TimeSpan StartUpTimeout { get; set; }
    public TimeSpan ShutDownTimeout { get; set; }
    public TimeSpan TimeToLive { get; set; }
    public bool FakeLogging { get; set; }
    public bool ValidateScopes { get; set; }
    public bool ValidateOnBuild { get; set; }
    public bool FakeRedaction { get; set; }
    public FakeHostOptions();
}
