// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Razor.Workspaces;

namespace Microsoft.CodeAnalysis.Host
{
    internal class RazorDocumentServiceProvider : IDocumentServiceProvider, IDocumentOperationService
    {
        private readonly DynamicDocumentContainer _documentContainer;
        private readonly object _lock;

        private ISpanMappingService _spanMappingService;
        private IDocumentExcerptService _excerptService;

        public RazorDocumentServiceProvider()
            : this(null)
        {
        }

        public RazorDocumentServiceProvider(DynamicDocumentContainer documentContainer)
        {
            _documentContainer = documentContainer;

            _lock = new object();
        }

        public bool CanApplyChange => false;

        public bool SupportDiagnostics => false;

        public TService GetService<TService>() where TService : class, IDocumentService
        {
            if (_documentContainer == null)
            {
                return this as TService;
            }

            if (typeof(TService) == typeof(ISpanMappingService))
            {
                if (_spanMappingService == null)
                {
                    lock (_lock)
                    {
                        if (_spanMappingService == null)
                        {
                            var spanMappingServiceObject = _documentContainer.GetMappingService();
                            _spanMappingService = (ISpanMappingService)spanMappingServiceObject;
                        }
                    }
                }

                return (TService)(object)_spanMappingService;
            }

            if (typeof(TService) == typeof(IDocumentExcerptService))
            {
                if (_excerptService == null)
                {
                    lock (_lock)
                    {
                        if (_excerptService == null)
                        {
                            var excerptServiceObject = _documentContainer.GetExcerptService();
                            _excerptService = (IDocumentExcerptService)excerptServiceObject;
                        }
                    }
                }

                return (TService)(object)_excerptService;
            }

            return this as TService;
        }
    }
}
