// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal sealed class RegistrationExtensionResult
    {
        public RegistrationExtensionResult(string serverCapability, object options)
        {
            if (serverCapability is null)
            {
                throw new ArgumentNullException(nameof(serverCapability));
            }

            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            ServerCapability = serverCapability;
            Options = options;
        }

        public string ServerCapability { get; }

        public object Options { get; }
    }
}
