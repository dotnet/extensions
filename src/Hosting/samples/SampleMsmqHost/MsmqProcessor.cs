// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Messaging;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SampleMsmqHost
{
    public interface IMsmqProcessor
    {
        Task ProcessMessageAsync(Message message, CancellationToken cancellationToken);
    }

    public class MsmqProcessor : IMsmqProcessor
    {
        private readonly ILogger<MsmqProcessor> _logger;

        public MsmqProcessor(ILogger<MsmqProcessor> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task ProcessMessageAsync(Message message, CancellationToken cancellationToken)
        {
            // we assume the message contains text encoded as UTF7
            using (var reader = new StreamReader(message.BodyStream, Encoding.UTF7))
            {
                var text = await reader.ReadToEndAsync();

                _logger.LogInformation("Received Message: {0}", text);
            }
        }

    }
}