// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal static class TagHelperDescriptorExtensions
    {
        public static bool IsAttributeDescriptor(this TagHelperDescriptor d)
        {
            return d.Metadata.TryGetValue(TagHelperMetadata.Common.ClassifyAttributesOnly, out var value) ||
                string.Equals(value, bool.TrueString, StringComparison.OrdinalIgnoreCase);
        }
    }
}
