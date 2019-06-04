// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.Logging.Console
{
    /// <summary>
    /// Format of ConsoleLogger messages.
    /// </summary>
    public enum ConsoleLoggerFormat
    {
        /// <summary>
        /// Default format.
        /// </summary>
        Default,
        /// <summary>
        /// systemd '&lt;pri&gt;message' format.
        /// </summary>
        Systemd
    }
}