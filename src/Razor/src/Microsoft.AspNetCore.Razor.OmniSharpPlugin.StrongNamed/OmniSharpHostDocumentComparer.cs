// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;

namespace Microsoft.AspNetCore.Razor.OmniSharpPlugin
{
    public sealed class OmniSharpHostDocumentComparer : IEqualityComparer<OmniSharpHostDocument>
    {
        public static readonly OmniSharpHostDocumentComparer Instance = new OmniSharpHostDocumentComparer();

        private OmniSharpHostDocumentComparer()
        {
        }

        public bool Equals(OmniSharpHostDocument x, OmniSharpHostDocument y) => 
            HostDocumentComparer.Instance.Equals(x.InternalHostDocument, y.InternalHostDocument);

        public int GetHashCode(OmniSharpHostDocument hostDocument) => 
            HostDocumentComparer.Instance.GetHashCode(hostDocument.InternalHostDocument);
    }
}
