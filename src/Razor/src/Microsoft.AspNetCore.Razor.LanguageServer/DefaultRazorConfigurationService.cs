// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Razor.Editor;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal class DefaultRazorConfigurationService : RazorConfigurationService
    {
        private readonly ClientNotifierServiceBase _server;
        private readonly ILogger _logger;

        public DefaultRazorConfigurationService(ClientNotifierServiceBase languageServer, ILoggerFactory loggerFactory)
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

        public async override Task<RazorLSPOptions> GetLatestOptionsAsync(CancellationToken cancellationToken)
        {
            try
            {
                var request = GenerateConfigParams();

                var response = await _server.SendRequestAsync("workspace/configuration", request);
                var result = await response.Returning<JObject[]>(cancellationToken);

                // LSP spec indicates result should be the same length as the number of ConfigurationItems we pass in.
                if (result == null || result.Length != request.Items.Count() || result[0] == null)
                {
                    _logger.LogWarning("Client failed to provide the expected configuration.");
                    return null;
                }

                var instance = BuildOptions(result);
                return instance;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to sync client configuration on the server: {ex}");
                return null;
            }
        }

        private static ConfigurationParams GenerateConfigParams()
        {
            // NOTE: Do not change the ordering of sections without updating
            // the code in the BuildOptions method below.
            return new ConfigurationParams()
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
                    new ConfigurationItem()
                    {
                        Section = "vs.editor.razor"
                    },
                }
            };
        }

        // Internal for testing
        internal RazorLSPOptions BuildOptions(JObject[] result)
        {
            ExtractVSCodeOptions(result, out var trace, out var enableFormatting, out var autoClosingTags);
            ExtractVSOptions(result, out var insertSpaces, out var tabSize);

            return new RazorLSPOptions(trace, enableFormatting, autoClosingTags, insertSpaces, tabSize);
        }

        private void ExtractVSCodeOptions(
            JObject[] result,
            out Trace trace,
            out bool enableFormatting,
            out bool autoClosingTags)
        {
            var razor = result[0];
            var html = result[1];

            trace = RazorLSPOptions.Default.Trace;
            if (razor.TryGetValue("trace", out var parsedTrace))
            {
                trace = GetObjectOrDefault(parsedTrace, trace);
            }

            enableFormatting = RazorLSPOptions.Default.EnableFormatting;
            if (razor.TryGetValue("format", out var parsedFormat))
            {
                if (parsedFormat is JObject jObject &&
                    jObject.TryGetValue("enable", out var parsedEnableFormatting))
                {
                    enableFormatting = GetObjectOrDefault(parsedEnableFormatting, enableFormatting);
                }
            }

            autoClosingTags = RazorLSPOptions.Default.AutoClosingTags;
            if (html.TryGetValue("autoClosingTags", out var parsedAutoClosingTags))
            {
                autoClosingTags = GetObjectOrDefault(parsedAutoClosingTags, autoClosingTags);
            }
        }

        private void ExtractVSOptions(
            JObject[] result,
            out bool insertSpaces,
            out int tabSize)
        {
            var vsEditor = result[2];

            insertSpaces = RazorLSPOptions.Default.InsertSpaces;
            if (vsEditor.TryGetValue(nameof(EditorSettings.IndentWithTabs), out var parsedInsertTabs))
            {
                insertSpaces = !GetObjectOrDefault(parsedInsertTabs, insertSpaces);
            }

            tabSize = RazorLSPOptions.Default.TabSize;
            if (vsEditor.TryGetValue(nameof(EditorSettings.IndentSize), out var parsedTabSize))
            {
                tabSize = GetObjectOrDefault(parsedTabSize, tabSize);
            }
        }

        private T GetObjectOrDefault<T>(JToken token, T defaultValue)
        {
            try
            {
                // JToken.ToObject could potentially throw here if the user provides malformed options.
                // If this occurs, catch the exception and return the default value.
                return token.ToObject<T>() ?? defaultValue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Malformed option: Token {token} cannot be converted to type {typeof(T)}.");
                return defaultValue;
            }
        }
    }
}
