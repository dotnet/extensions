// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.Logging.AzureWebAppDiagnostics
{
    /// <summary>
    /// Settings for <see cref="AzureWebAppDiagnosticsLoggerProvider"/>.
    /// </summary>
    public class AzureWebAppDiagnosticsSettings
    {
        /// <summary>
        /// Gets or sets a strictly positive value representing the maximum log size in bytes. Once the log is full, no more message will be appended.
        /// </summary>
        public int FileSizeLimit { get; set; } = 10 * 1024 * 1024;

        /// <summary>
        /// Gets or sets a strictly positive value representing the maximum retained file count.
        /// </summary>
        public int RetainedFileCountLimit { get; set; } = 2;

        /// <summary>
        /// Gets or sets a message template describing the output messages.
        /// </summary>
        public string OutputTemplate { get; set; } = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level}] {Message}{NewLine}{Exception}";

        /// <summary>
        /// Gets or sets a maximum number of events to include in a single blob append batch.
        /// </summary>
        public int BlobBatchSize { get; set; } = 32;

        /// <summary>
        /// Gets or sets a time to wait between checking for blob log batches.
        /// </summary>
        public TimeSpan BlobCommitPeriod { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Gets or sets the last section of log blob name.
        /// </summary>
        public string BlobName { get; set; } = "applicationLog.txt";

        /// Gets of sets the maximum size of the background log message queue.
        public int BackgroundQueueSize { get; set; }
    }
}