// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Razor.Completion;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Editor.Razor.Completion
{
    internal static class PropertyCollectionExtensions
    {
        public static object CompletionItemKindsKey = new object();

        public static void SetCompletionItemKinds(this PropertyCollection properties, ICollection<RazorCompletionItemKind> completionItemKinds)
        {
            if (properties is null)
            {
                throw new ArgumentNullException(nameof(properties));
            }

            if (completionItemKinds is null)
            {
                throw new ArgumentNullException(nameof(completionItemKinds));
            }

            properties[CompletionItemKindsKey] = completionItemKinds;
        }

        public static bool TryGetCompletionItemKinds(this PropertyCollection properties, out ICollection<RazorCompletionItemKind> completionItemKinds)
        {
            if (properties is null)
            {
                throw new ArgumentNullException(nameof(properties));
            }

            return properties.TryGetProperty(CompletionItemKindsKey, out completionItemKinds);
        }
    }
}