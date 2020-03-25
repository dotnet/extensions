// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Razor.Workspaces;

namespace Microsoft.CodeAnalysis.Razor
{
    [Export(typeof(DocumentServiceProviderFactory))]
    internal class DefaultDocumentServiceProviderFactory : DocumentServiceProviderFactory
    {
        public override IDocumentServiceProvider Create(DynamicDocumentContainer documentContainer)
        {
            if (documentContainer == null)
            {
                throw new ArgumentNullException(nameof(documentContainer));
            }

            return new RazorDocumentServiceProvider(documentContainer);
        }

        public override IDocumentServiceProvider CreateEmpty()
        {
            return new RazorDocumentServiceProvider();
        }
    }
}
