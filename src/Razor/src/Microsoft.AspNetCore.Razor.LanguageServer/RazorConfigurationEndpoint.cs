// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Workspace;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal class RazorConfigurationEndpoint : IDidChangeConfigurationHandler
    {
        private readonly RazorLSPOptionsMonitor _optionsMonitor;
        private readonly ILogger _logger;
        private DidChangeConfigurationCapability _capability;

        public RazorConfigurationEndpoint(RazorLSPOptionsMonitor optionsMonitor, ILoggerFactory loggerFactory)
        {
            if (optionsMonitor is null)
            {
                throw new ArgumentNullException(nameof(optionsMonitor));
            }

            if (loggerFactory is null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _optionsMonitor = optionsMonitor;
            _logger = loggerFactory.CreateLogger<RazorConfigurationEndpoint>();
        }

        public object GetRegistrationOptions()
        {
            return new TextDocumentRegistrationOptions()
            {
                DocumentSelector = RazorDefaults.Selector
            };
        }

        public async Task<Unit> Handle(DidChangeConfigurationParams request, CancellationToken cancellationToken)
        {
            _logger.LogTrace("Settings changed. Updating the server.");

            await _optionsMonitor.UpdateAsync(cancellationToken);

            return new Unit();
        }

        public void SetCapability(DidChangeConfigurationCapability capability)
        {
            _capability = capability;
        }
    }
}
