// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    [AppliesTo("(DotNetCoreRazor & DotNetCoreRazorConfiguration) | ((DotNetCoreRazor | DotNetCoreWeb) & !DotNetCoreRazorConfiguration)")]
    [Export(ExportContractNames.Scopes.UnconfiguredProject, typeof(IProjectDynamicLoadComponent))]
    internal class LSPRazorProjectHost : IProjectDynamicLoadComponent
    {
        private readonly LSPEditorFeatureDetector _featureDetector;
        private readonly List<Lazy<ILanguageClient, ILanguageClientMetadata>> _applicableLanguageClients;
        private readonly Lazy<ILanguageClientBroker> _languageClientBroker;

        [ImportingConstructor]
        public LSPRazorProjectHost(
            LSPEditorFeatureDetector featureDetector,
            Lazy<ILanguageClientBroker> languageClientBroker,
            [ImportMany] IEnumerable<Lazy<ILanguageClient, IDictionary<string, object>>> languageClients)
        {
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

            _featureDetector = featureDetector;
            _languageClientBroker = languageClientBroker;
            _applicableLanguageClients = GetApplicableClients(languageClients);
        }

        public Task LoadAsync()
        {
            if (!_featureDetector.IsLSPEditorFeatureEnabled())
            {
                return Task.CompletedTask;
            }

            foreach (var client in _applicableLanguageClients)
            {
                _languageClientBroker.Value.LoadAsync(client.Metadata, client.Value).Forget();
            }

            return Task.CompletedTask;
        }

        public Task UnloadAsync()
        {
            return Task.CompletedTask;
        }

        private static List<Lazy<ILanguageClient, ILanguageClientMetadata>> GetApplicableClients(IEnumerable<Lazy<ILanguageClient, IDictionary<string, object>>> languageClients)
        {
            var applicableContentTypes = new[]
            {
                RazorLSPContentTypeDefinition.Name,
                CSharpVirtualDocumentFactory.CSharpLSPContentTypeName,
                HtmlVirtualDocumentFactory.HtmlLSPContentTypeName,
            };

            var applicableClients = new List<Lazy<ILanguageClient, ILanguageClientMetadata>>();
            foreach (var client in languageClients)
            {
                if (!client.Metadata.TryGetValue(nameof(ILanguageClientMetadata.ContentTypes), out var contentTypeValue) ||
                    !(contentTypeValue is IEnumerable<string> contentTypes) ||
                    !contentTypes.Intersect(applicableContentTypes).Any())
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
