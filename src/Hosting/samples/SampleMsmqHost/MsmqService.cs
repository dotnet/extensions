// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SampleMsmqHost
{
    public class MsmqService : BackgroundService
    {
        public ILogger<MsmqService> Logger { get; }

        public IMsmqConnection Connection { get; }

        public IMsmqProcessor Processor { get; }

        public MsmqService(ILogger<MsmqService> logger, IMsmqConnection connection, IMsmqProcessor processor)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            Connection = connection ?? throw new ArgumentNullException(nameof(connection));
            Processor = processor ?? throw new ArgumentNullException(nameof(processor));
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            Logger.LogInformation("Begin Receive Loop");
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    using (var message = await Connection.ReceiveAsync(cancellationToken))
                    {
                        await Processor.ProcessMessageAsync(message, cancellationToken);
                    }
                }
            }
            finally
            {
                Logger.LogInformation("End Receive Loop");
            }
        }

    }
}