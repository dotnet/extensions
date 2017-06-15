// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Microsoft.Extensions.Logging.AzureAppServices.Internal
{
    /// <inheritdoc />
    public class BlobAppendReferenceWrapper : ICloudAppendBlob
    {
        private readonly CloudAppendBlob _cloudAppendBlob;

        /// <summary>
        /// Creates new instance of <see cref="BlobAppendReferenceWrapper"/>.
        /// </summary>
        /// <param name="cloudAppendBlob">The <see cref="CloudAppendBlob"/> instance to wrap.</param>
        public BlobAppendReferenceWrapper(CloudAppendBlob cloudAppendBlob)
        {
            _cloudAppendBlob = cloudAppendBlob;
        }
        /// <inheritdoc />
        public async Task<Stream> OpenWriteAsync(CancellationToken cancellationToken)
        {
            return await _cloudAppendBlob.OpenWriteAsync(
                createNew: false,
                accessCondition: null,
                options: null,
                operationContext: null,
                cancellationToken: cancellationToken);
        }

        /// <inheritdoc />
        public async Task CreateAsync(CancellationToken cancellationToken)
        {
            await _cloudAppendBlob.CreateOrReplaceAsync(
                AccessCondition.GenerateIfNotExistsCondition(),
                options: null,
                operationContext: null,
                cancellationToken: cancellationToken);
        }
    }
}