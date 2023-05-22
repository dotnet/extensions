// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Hosting.Testing.StartupInitialization.Test;

public class Database
{
    private readonly ILogger _logger;

    public const string LogMessage = "HEY I WAS INITIALIZED AT STARTUP IN ASYNC WAY HEHE";

    public Database(ILogger<Database> logger)
    {
        _logger = logger;
    }

    public Task Initialize()
    {
        _logger.LogInformation(LogMessage);

        return Task.CompletedTask;
    }
}
