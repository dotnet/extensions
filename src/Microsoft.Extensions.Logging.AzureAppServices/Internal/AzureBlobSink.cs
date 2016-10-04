// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Sinks.PeriodicBatching;

namespace Microsoft.Extensions.Logging.AzureAppServices.Internal
{
    /// <summary>
    /// The <see cref="ILogEventSink"/> implemenation that stores messages by appending them to Azure Blob in batches.
    /// </summary>
    public class AzureBlobSink : PeriodicBatchingSink
    {
        private readonly string _appName;
        private readonly string _fileName;
        private readonly ITextFormatter _formatter;
        private readonly Func<string, ICloudAppendBlob> _blobReferenceFactory;

        /// <summary>
        /// Creates a new instance of <see cref="AzureBlobSink"/>
        /// </summary>
        /// <param name="blobReferenceFactory">The container to store logs to.</param>
        /// <param name="appName">The application name to use in blob path generation.</param>
        /// <param name="fileName">The last segment of blob name.</param>
        /// <param name="formatter">The <see cref="ITextFormatter"/> for log messages.</param>
        /// <param name="batchSizeLimit">The maximum number of events to include in a single batch.</param>
        /// <param name="period">The time to wait between checking for event batches.</param>
        public AzureBlobSink(Func<string, ICloudAppendBlob> blobReferenceFactory,
            string appName,
            string fileName,
            ITextFormatter formatter,
            int batchSizeLimit,
            TimeSpan period) : base(batchSizeLimit, period)
        {
            if (appName == null)
            {
                throw new ArgumentNullException(nameof(appName));
            }
            if (fileName == null)
            {
                throw new ArgumentNullException(nameof(fileName));
            }
            if (formatter == null)
            {
                throw new ArgumentNullException(nameof(formatter));
            }
            if (batchSizeLimit <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(batchSizeLimit), $"{nameof(batchSizeLimit)} should be a positive number.");
            }
            if (period <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(period), $"{nameof(period)} should be longer than zero.");
            }

            _appName = appName;
            _fileName = fileName;
            _formatter = formatter;
            _blobReferenceFactory = blobReferenceFactory;
        }

        /// <inheritdoc />
        protected override async Task EmitBatchAsync(IEnumerable<LogEvent> events)
        {
            var eventGroups = events.GroupBy(GetBlobKey);
            foreach (var eventGroup in eventGroups)
            {
                var key = eventGroup.Key;
                var blobName = $"{_appName}/{key.Item1}/{key.Item2:00}/{key.Item3:00}/{key.Item4:00}/{_fileName}";

                var blob = _blobReferenceFactory(blobName);

                Stream stream;
                try
                {
                    stream = await blob.OpenWriteAsync();
                }
                // Blob does not exist
                catch (StorageException ex) when (ex.RequestInformation.HttpStatusCode == 404)
                {
                    await blob.CreateAsync();
                    stream = await blob.OpenWriteAsync();
                }

                using (var writer = new StreamWriter(stream))
                {
                    foreach (var logEvent in eventGroup)
                    {
                        _formatter.Format(logEvent, writer);
                    }
                }
            }
        }

        private Tuple<int,int,int,int> GetBlobKey(LogEvent e)
        {
            return Tuple.Create(e.Timestamp.Year,
                e.Timestamp.Month,
                e.Timestamp.Day,
                e.Timestamp.Hour);
        }
    }
}