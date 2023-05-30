// Assembly 'Microsoft.AspNetCore.ConnectionTimeout'

using System;
using System.Runtime.CompilerServices;
using Microsoft.Shared.Data.Validation;

namespace Microsoft.AspNetCore.Connections;

public class ConnectionTimeoutOptions
{
    [TimeSpan(0, Exclusive = true)]
    public TimeSpan Timeout { get; set; }
    public ConnectionTimeoutOptions();
}
