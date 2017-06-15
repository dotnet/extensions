// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Microsoft.Extensions.Logging.AzureAppServices.Internal
{
    /// <summary>
    /// The <see cref="ILoggerProvider"/> implementation that stores messages by appending them to Azure Blob in batches.
    /// </summary>
    [ProviderAlias("AzureAppServicesBlob")]
    public class BlobLoggerProvider : BatchingLoggerProvider
    {
        private readonly string _appName;
        private readonly string _fileName;
        private readonly Func<string, ICloudAppendBlob> _blobReferenceFactory;

        /// <summary>
        /// Creates a new instance of <see cref="BlobLoggerProvider"/>
        /// </summary>
        /// <param name="options"></param>
        public BlobLoggerProvider(IOptionsMonitor<AzureBlobLoggerOptions> options)
            : this(options,
                   GetDefaultBlobReferenceFactory(options.CurrentValue))
        {
        }

        private static Func<string, ICloudAppendBlob> GetDefaultBlobReferenceFactory(AzureBlobLoggerOptions options)
        {
            CloudBlobContainer container = null;
            // Delay initialize container in case logger starts disabled
            return name =>
            {
                container = container ?? new CloudBlobContainer(new Uri(options.ContainerUrl));
                return new BlobAppendReferenceWrapper(container.GetAppendBlobReference(name));
            };
        }

        /// <summary>
        /// Creates a new instance of <see cref="BlobLoggerProvider"/>
        /// </summary>
        /// <param name="blobReferenceFactory">The container to store logs to.</param>
        /// <param name="options"></param>
        public BlobLoggerProvider(
            IOptionsMonitor<AzureBlobLoggerOptions> options,
            Func<string, ICloudAppendBlob> blobReferenceFactory) :
            base(options)
        {
            var value = options.CurrentValue;
            _appName = value.ApplicationName;
            _fileName = value.ApplicationInstanceId + "_" + value.BlobName;
            _blobReferenceFactory = blobReferenceFactory;
        }

        protected override async Task WriteMessagesAsync(IEnumerable<LogMessage> messages, CancellationToken cancellationToken)
        {
            var eventGroups = messages.GroupBy(GetBlobKey);
            foreach (var eventGroup in eventGroups)
            {
                var key = eventGroup.Key;
                var blobName = $"{_appName}/{key.Year}/{key.Month:00}/{key.Day:00}/{key.Hour:00}/{_fileName}";

                var blob = _blobReferenceFactory(blobName);

                Stream stream;
                try
                {
                    stream = await blob.OpenWriteAsync(cancellationToken);
                }
                // Blob does not exist
                catch (StorageException ex) when (ex.RequestInformation.HttpStatusCode == 404)
                {
                    await blob.CreateAsync(cancellationToken);
                    stream = await blob.OpenWriteAsync(cancellationToken);
                }

                using (var writer = new StreamWriter(stream))
                {
                    foreach (var logEvent in eventGroup)
                    {
                        writer.Write(logEvent.Message);
                    }
                }
            }
        }

        private (int Year, int Month, int Day, int Hour) GetBlobKey(LogMessage e)
        {
            return (e.Timestamp.Year,
                e.Timestamp.Month,
                e.Timestamp.Day,
                e.Timestamp.Hour);
        }
    }
}