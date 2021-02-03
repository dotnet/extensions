// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Test.Formatting
{
    internal class FakeContentTypeRegistryService : IContentTypeRegistryService
    {
        private readonly object _lock = new object();
        private readonly Dictionary<string, ImmutableArray<string>> _contentTypeDefinitions = new Dictionary<string, ImmutableArray<string>>();
        private readonly Dictionary<string, IContentType> _contentTypes = new Dictionary<string, IContentType>();

        public FakeContentTypeRegistryService()
        {
            // Standard content types normally defined in BufferFactoryService
            AddContentType(StandardContentTypeNames.Any, Array.Empty<string>());
            AddContentType(StandardContentTypeNames.Text, new[] { StandardContentTypeNames.Any });
            AddContentType(StandardContentTypeNames.Projection, new[] { StandardContentTypeNames.Any });
            AddContentType(StandardContentTypeNames.Inert, Array.Empty<string>());
            AddContentType("plaintext", new[] { StandardContentTypeNames.Text });
            AddContentType(StandardContentTypeNames.Code, new[] { StandardContentTypeNames.Text });
        }

        public IContentType UnknownContentType => throw new NotImplementedException();

        public IEnumerable<IContentType> ContentTypes => throw new NotImplementedException();

        public IContentType AddContentType(string typeName, IEnumerable<string> baseTypeNames)
        {
            lock (_lock)
            {
                // Before adding to contentTypeDefinitions, make sure we can resolve the base types
                foreach (var baseType in baseTypeNames)
                {
                    _ = GetContentType(baseType);
                }

                _contentTypeDefinitions.Add(typeName, ImmutableArray.CreateRange(baseTypeNames));
                return GetContentType(typeName);
            }
        }

        public IContentType GetContentType(string typeName)
        {
            lock (_lock)
            {
                if (_contentTypes.TryGetValue(typeName, out var contentType))
                    return contentType;

                if (!_contentTypeDefinitions.TryGetValue(typeName, out var baseTypes))
                    throw new ArgumentException($"Content type '{typeName}' was not found.", nameof(typeName));

                contentType = new FakeContentType(typeName, ImmutableArray.CreateRange(baseTypes.Select(baseType => GetContentType(baseType))));
                _contentTypes[typeName] = contentType;
                return contentType;
            }
        }

        public void RemoveContentType(string typeName)
        {
            throw new NotImplementedException();
        }
    }
}
