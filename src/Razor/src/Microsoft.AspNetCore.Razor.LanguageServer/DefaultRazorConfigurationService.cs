// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal class DefaultRazorConfigurationService : RazorConfigurationService
    {
        private readonly ILanguageServer _server;
        private readonly ILogger _logger;

        public DefaultRazorConfigurationService(ILanguageServer languageServer, ILoggerFactory loggerFactory)
        {
            if (languageServer is null)
            {
                throw new ArgumentNullException(nameof(languageServer));
            }

            if (loggerFactory is null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _server = languageServer;
            _logger = loggerFactory.CreateLogger<DefaultRazorConfigurationService>();
        }

        public async override Task<RazorLSPOptions> GetLatestOptionsAsync()
        {
            try
            {
                var request = new ConfigurationParams()
                {
                    Items = new[]
                    {
                        new ConfigurationItem()
                        {
                            Section = "razor"
                        },
                        new ConfigurationItem()
                        {
                            Section = "html"
                        },
                    }
                };

                var result = await _server.Client.SendRequest<ConfigurationParams, object[]>("workspace/configuration", request);
                if (result == null || result.Length < 2 || result[0] == null)
                {
                    _logger.LogWarning("Client failed to provide the expected configuration.");
                    return null;
                }

                var builder = new ConfigurationBuilder();

                var razorJsonString = result[0].ToString();
                using var razorStream = new MemoryStream(Encoding.UTF8.GetBytes(razorJsonString));
                builder.AddJsonStream(razorStream);

                var htmlJsonString = result[1].ToString();
                using var htmlStream = new MemoryStream(Encoding.UTF8.GetBytes(htmlJsonString));
                builder.AddJsonStream(htmlStream);

                var config = builder.Build();

                var instance = BuildOptions(config);
                return instance;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to sync client configuration on the server: {ex}");
                return null;
            }
        }

        private RazorLSPOptions BuildOptions(IConfiguration config)
        {
            var instance = RazorLSPOptions.Default;

            Enum.TryParse(config["trace"], out Trace trace);

            var enableFormatting = instance.EnableFormatting;
            if (bool.TryParse(config["format:enable"], out var parsedEnableFormatting))
            {
                enableFormatting = parsedEnableFormatting;
            }

            var autoClosingTags = instance.AutoClosingTags;
            if (bool.TryParse(config["autoClosingTags"], out var parsedAutoClosingTags))
            {
                autoClosingTags = parsedAutoClosingTags;
            }

            return new RazorLSPOptions(trace, enableFormatting, autoClosingTags);
        }
    }
}
