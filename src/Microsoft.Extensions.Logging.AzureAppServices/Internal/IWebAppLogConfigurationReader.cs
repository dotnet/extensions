// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.Logging.AzureAppServices.Internal
{

    /// <summary>
    /// The contract for a WebApp configuration reader.
    /// </summary>
    public interface IWebAppLogConfigurationReader : IDisposable
    {
        /// <summary>
        /// Triggers when the configuration has changed.
        /// </summary>
        event EventHandler<WebAppLogConfiguration> OnConfigurationChanged;

        /// <summary>
        /// The current value of the configuration.
        /// </summary>
        WebAppLogConfiguration Current { get; }
    }
}
