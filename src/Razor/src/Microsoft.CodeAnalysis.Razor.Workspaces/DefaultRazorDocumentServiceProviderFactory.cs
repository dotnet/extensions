// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;

namespace Microsoft.CodeAnalysis.Razor.Workspaces
{
    [Shared]
    [Export(typeof(RazorDocumentServiceProviderFactory))]
    internal class DefaultRazorDocumentServiceProviderFactory : RazorDocumentServiceProviderFactory
    {
        public override IRazorDocumentServiceProvider Create(DynamicDocumentContainer documentContainer)
        {
            if (documentContainer is null)
            {
                throw new ArgumentNullException(nameof(documentContainer));
            }

            return new RazorDocumentServiceProvider(documentContainer);
        }

        public override IRazorDocumentServiceProvider CreateEmpty()
        {
            return new RazorDocumentServiceProvider();
        }
    }
}
