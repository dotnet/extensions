// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    [AppliesTo("(DotNetCoreRazor & DotNetCoreRazorConfiguration) | ((DotNetCoreRazor | DotNetCoreWeb) & !DotNetCoreRazorConfiguration)")]
    [Export(ExportContractNames.Scopes.UnconfiguredProject, typeof(IProjectDynamicLoadComponent))]
    internal class LSPRazorProjectHost : IProjectDynamicLoadComponent
    {
        private static readonly string[] _applicableContentTypes = new string[]
        {
            RazorLSPConstants.RazorLSPContentTypeName,
            RazorLSPConstants.CSharpLSPContentTypeName,
            RazorLSPConstants.HtmlLSPContentTypeName,
        };

        private readonly JoinableTaskFactory _joinableTaskFactory;
        private readonly LSPEditorFeatureDetector _featureDetector;
        private readonly Lazy<ILanguageClientBroker> _languageClientBroker;
        private readonly IEnumerable<Lazy<ILanguageClient, IDictionary<string, object>>> _languageClients;

        [ImportingConstructor]
        public LSPRazorProjectHost(
            JoinableTaskContext joinableTaskContext,
            LSPEditorFeatureDetector featureDetector,
            Lazy<ILanguageClientBroker> languageClientBroker,
            [ImportMany] IEnumerable<Lazy<ILanguageClient, IDictionary<string, object>>> languageClients)
        {
            if (joinableTaskContext is null)
            {
                throw new ArgumentNullException(nameof(joinableTaskContext));
            }

            if (featureDetector is null)
            {
                throw new ArgumentNullException(nameof(featureDetector));
            }

            if (languageClientBroker is null)
            {
                throw new ArgumentNullException(nameof(languageClientBroker));
            }

            if (languageClients is null)
            {
                throw new ArgumentNullException(nameof(languageClients));
            }

            _joinableTaskFactory = joinableTaskContext.Factory;
            _featureDetector = featureDetector;
            _languageClientBroker = languageClientBroker;
            _languageClients = languageClients;
        }

        private List<Lazy<ILanguageClient, ILanguageClientMetadata>> ApplicableLanguageClients { get; set; }

        public async Task LoadAsync()
        {
            await _joinableTaskFactory.SwitchToMainThreadAsync();

            if (!_featureDetector.IsLSPEditorFeatureEnabled())
            {
                return;
            }

            ApplicableLanguageClients = GetApplicableClients(_languageClients);

            foreach (var client in ApplicableLanguageClients)
            {
                _languageClientBroker.Value.LoadAsync(client.Metadata, client.Value).Forget();
            }
        }

        public Task UnloadAsync()
        {
            return Task.CompletedTask;
        }

        private static List<Lazy<ILanguageClient, ILanguageClientMetadata>> GetApplicableClients(IEnumerable<Lazy<ILanguageClient, IDictionary<string, object>>> languageClients)
        {
            var applicableClients = new List<Lazy<ILanguageClient, ILanguageClientMetadata>>();
            foreach (var client in languageClients)
            {
                if (!client.Metadata.TryGetValue(nameof(ILanguageClientMetadata.ContentTypes), out var contentTypeValue) ||
                    !(contentTypeValue is IEnumerable<string> contentTypes) ||
                    !contentTypes.Intersect(_applicableContentTypes).Any())
                {
                    continue;
                }

                string clientName = null;
                if (client.Metadata.TryGetValue(nameof(ILanguageClientMetadata.ClientName), out var clientNameValue))
                {
                    clientName = clientNameValue.ToString();
                }

                applicableClients.Add(new Lazy<ILanguageClient, ILanguageClientMetadata>(
                    () => { return client.Value; },
                    new LanguageClientMetadata(clientName, contentTypes)));
            }

            return applicableClients;
        }

        private class LanguageClientMetadata : ILanguageClientMetadata
        {
            public LanguageClientMetadata(string clientName, IEnumerable<string> contentTypes)
            {
                ClientName = clientName;
                ContentTypes = contentTypes;
            }

            public string ClientName { get; }

            public IEnumerable<string> ContentTypes { get; }
        }
    }
}
