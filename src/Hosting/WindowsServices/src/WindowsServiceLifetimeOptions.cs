// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.Hosting
{
    public class WindowsServiceLifetimeOptions
    {
        /// <summary>
        /// The name used to identify the service to the system.
        /// </summary>
        public string ServiceName { get; set; } = string.Empty;
    }
}
