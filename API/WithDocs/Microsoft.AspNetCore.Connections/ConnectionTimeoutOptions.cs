// Assembly 'Microsoft.AspNetCore.ConnectionTimeout'

using System;
using System.Runtime.CompilerServices;
using Microsoft.Shared.Data.Validation;

namespace Microsoft.AspNetCore.Connections;

/// <summary>
/// Options to configure the connection timeout middleware.
/// </summary>
public class ConnectionTimeoutOptions
{
    /// <summary>
    /// Gets or sets the time after which a connection will be shut down.
    /// </summary>
    /// <value>
    /// The default value is 5 minutes.
    /// </value>
    [TimeSpan(0, Exclusive = true)]
    public TimeSpan Timeout { get; set; }

    public ConnectionTimeoutOptions();
}
