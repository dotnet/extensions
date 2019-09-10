// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Completion
{
    internal class ElementDescriptionInfo
    {
        public static readonly ElementDescriptionInfo Default = new ElementDescriptionInfo(Array.Empty<TagHelperDescriptionInfo>());

        public ElementDescriptionInfo(IReadOnlyList<TagHelperDescriptionInfo> associatedTagHelperDescriptions)
        {
            if (associatedTagHelperDescriptions == null)
            {
                throw new ArgumentNullException(nameof(associatedTagHelperDescriptions));
            }

            AssociatedTagHelperDescriptions = associatedTagHelperDescriptions;
        }

        public IReadOnlyList<TagHelperDescriptionInfo> AssociatedTagHelperDescriptions { get; }
    }
}
