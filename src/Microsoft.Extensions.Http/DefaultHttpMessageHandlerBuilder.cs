// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net.Http;

namespace Microsoft.Extensions.Http
{
    internal class DefaultHttpMessageHandlerBuilder : HttpMessageHandlerBuilder
    {
        public override HttpMessageHandler PrimaryHandler { get; set; } = new HttpClientHandler();

        public override IList<DelegatingHandler> AdditionalHandlers { get; } = new List<DelegatingHandler>();

        // Similar to https://github.com/aspnet/AspNetWebStack/blob/master/src/System.Net.Http.Formatting/HttpClientFactory.cs#L58
        public override HttpMessageHandler Build()
        {
            if (PrimaryHandler == null)
            {
                var message = Resources.FormatDefaultHttpMessageHandlerBuilder_PrimaryHandlerIsNull(nameof(PrimaryHandler));
                throw new InvalidOperationException(message);
            }

            var next = PrimaryHandler;
            for (var i = AdditionalHandlers.Count - 1; i >= 0; i--)
            {
                var handler = AdditionalHandlers[i];
                if (handler == null)
                {
                    var message = Resources.FormatDefaultHttpMessageHandlerBuilder_AdditionalHandlerIsNull(nameof(AdditionalHandlers));
                    throw new InvalidOperationException(message);
                }

                // Checking for this allows us to catch cases where someone has tried to re-use a handler. That really won't
                // work the way you want and it can be tricky for callers to figure out.
                if (handler.InnerHandler != null)
                {
                    var message = Resources.FormatDefaultHttpMessageHandlerBuilder_AdditionHandlerIsInvalid(
                        nameof(DelegatingHandler.InnerHandler),
                        nameof(DelegatingHandler),
                        nameof(AdditionalHandlers),
                        Environment.NewLine,
                        handler);
                    throw new InvalidOperationException(message);
                }

                handler.InnerHandler = next;
                next = handler;
            }

            return next;
        }
    }
}
