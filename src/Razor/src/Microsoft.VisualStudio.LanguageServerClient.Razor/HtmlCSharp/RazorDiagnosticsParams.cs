// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.Extensions.Internal;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.HtmlCSharp
{
    // Note: This type should be kept in sync with the one in Razor.LanguageServer assembly.
    internal class RazorDiagnosticsParams : IEquatable<RazorDiagnosticsParams>
    {
        public RazorLanguageKind Kind { get; set; }

        public Uri RazorDocumentUri { get; set; }

        public Diagnostic[] Diagnostics { get; set; }

        public bool Equals(RazorDiagnosticsParams other)
        {
            return
                other != null &&
                Kind == other.Kind &&
                RazorDocumentUri == other.RazorDocumentUri &&
                Enumerable.SequenceEqual(Diagnostics, other.Diagnostics);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as RazorDiagnosticsParams);
        }

        public override int GetHashCode()
        {
            var hash = new HashCodeCombiner();
            hash.Add(Kind);
            hash.Add(RazorDocumentUri);
            hash.Add(Diagnostics);
            return hash;
        }
    }
}
