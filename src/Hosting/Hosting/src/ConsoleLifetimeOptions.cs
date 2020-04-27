// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.Hosting
{
    public class ConsoleLifetimeOptions
    {
        /// <summary>
        /// Indicates if host lifetime status messages should be supressed such as on startup.
        /// The default is false.
        /// </summary>
        public bool SuppressStatusMessages { get; set; }

        /// <summary>
        /// Indicates if host lifetime shouldn't handle the <see cref="Console.CancelKeyPress"/> event.
        /// </summary>
        public bool IgnoreCancelKeyPress { get; set; }
    }
}
