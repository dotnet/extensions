// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.Extensions.Internal;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.HtmlCSharp
{
    // Note: This type should be kept in sync with the one in Razor.LanguageServer assembly.
    internal class RazorDiagnosticsResponse : IEquatable<RazorDiagnosticsResponse>
    {
        public Diagnostic[] Diagnostics { get; set; }

        public int? HostDocumentVersion { get; set; }

        public bool Equals(RazorDiagnosticsResponse other)
        {
            return Enumerable.SequenceEqual(Diagnostics, other.Diagnostics) &&
                HostDocumentVersion == other.HostDocumentVersion;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as RazorDiagnosticsResponse);
        }

        public override int GetHashCode()
        {
            var hash = new HashCodeCombiner();
            hash.Add(Diagnostics);
            hash.Add(HostDocumentVersion);
            return hash;
        }
    }
}
