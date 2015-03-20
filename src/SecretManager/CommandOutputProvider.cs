// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.Logging;

namespace SecretManager
{
    public class CommandOutputProvider : ILoggerProvider
    {
        public ILogger CreateLogger(string name)
        {
            return new CommandOutputLogger(this);
        }

        public LogLevel LogLevel { get; set; } = LogLevel.Information;
    }
}