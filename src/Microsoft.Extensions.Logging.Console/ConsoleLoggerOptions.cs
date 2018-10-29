// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.Logging.Console
{
    public class ConsoleLoggerOptions
    {
        public bool IncludeScopes { get; set; }
        public bool DisableColors { get; set; }

        /// <summary>
        /// Gets or sets format string used to format timestamp in logging messages. Defaults to <c>null</c>
        /// </summary>
        public string TimestampFormat { get; set; }
    }
}