// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.Extensions.Internal;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.HtmlCSharp
{
    internal class RazorMapToDocumentEditsParams : IEquatable<RazorMapToDocumentEditsParams>
    {
        public RazorLanguageKind Kind { get; set; }

        public Uri RazorDocumentUri { get; set; }

        public TextEdit[] ProjectedTextEdits { get; set; }

        public bool ShouldFormat { get; set; }

        public FormattingOptions FormattingOptions { get; set; }

        // Everything below this is for testing purposes only.
        public bool Equals(RazorMapToDocumentEditsParams other)
        {
            if (object.ReferenceEquals(this, other))
            {
                return true;
            }

            return
                other != null &&
                Kind == other.Kind &&
                RazorDocumentUri == other.RazorDocumentUri &&
                Enumerable.SequenceEqual(ProjectedTextEdits?.Select(p => p.NewText), other.ProjectedTextEdits?.Select(p => p.NewText)) &&
                Enumerable.SequenceEqual(ProjectedTextEdits?.Select(p => p.Range), other.ProjectedTextEdits?.Select(p => p.Range)) &&
                ShouldFormat == other.ShouldFormat &&
                IsEqual(other.FormattingOptions);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as RazorMapToDocumentEditsParams);
        }

        public override int GetHashCode()
        {
            var hash = new HashCodeCombiner();
            hash.Add(Kind);
            hash.Add(RazorDocumentUri);
            hash.Add(ProjectedTextEdits);
            return hash;
        }

        private bool IsEqual(FormattingOptions other)
        {
            if (FormattingOptions == null || other == null)
            {
                return FormattingOptions == other;
            }

            return
                FormattingOptions.InsertSpaces == other.InsertSpaces &&
                FormattingOptions.TabSize == other.TabSize &&
                (object.ReferenceEquals(FormattingOptions.OtherOptions, other.OtherOptions) ||
                ((FormattingOptions.OtherOptions != null && other.OtherOptions != null) &&
                FormattingOptions.OtherOptions.OrderBy(k => k.Key).SequenceEqual(other.OtherOptions.OrderBy(k => k.Key))));
        }
    }
}
