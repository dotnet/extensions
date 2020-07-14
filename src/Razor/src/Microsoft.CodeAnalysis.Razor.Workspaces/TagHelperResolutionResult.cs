// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor.Serialization;
using Newtonsoft.Json;

namespace Microsoft.CodeAnalysis.Razor
{
    [JsonConverter(typeof(TagHelperResolutionResultJsonConverter))]
    internal sealed class TagHelperResolutionResult
    {
        internal static readonly TagHelperResolutionResult Empty = new TagHelperResolutionResult(Array.Empty<TagHelperDescriptor>(), Array.Empty<RazorDiagnostic>());

        public TagHelperResolutionResult(IReadOnlyList<TagHelperDescriptor> descriptors, IReadOnlyList<RazorDiagnostic> diagnostics)
        {
            Descriptors = descriptors;
            Diagnostics = diagnostics;
        }

        public IReadOnlyList<TagHelperDescriptor> Descriptors { get; }

        public IReadOnlyList<RazorDiagnostic> Diagnostics { get; }
    }
}