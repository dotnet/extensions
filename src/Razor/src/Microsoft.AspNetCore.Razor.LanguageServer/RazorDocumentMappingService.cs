// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;
using Position = OmniSharp.Extensions.LanguageServer.Protocol.Models.Position;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal abstract class RazorDocumentMappingService
    {
        public abstract bool TryMapFromProjectedDocumentRange(RazorCodeDocument codeDocument, Range projectedRange, out Range originalRange);

        public abstract bool TryMapToProjectedDocumentRange(RazorCodeDocument codeDocument, Range originalRange, out Range projectedRange);

        public abstract bool TryMapFromProjectedDocumentPosition(RazorCodeDocument codeDocument, int csharpAbsoluteIndex, out Position originalPosition, out int originalIndex);

        public abstract bool TryMapToProjectedDocumentPosition(RazorCodeDocument codeDocument, int absoluteIndex, out Position projectedPosition, out int projectedIndex);
    }
}
