// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Completion
{
    internal class AttributeDescriptionInfo
    {
        public static readonly AttributeDescriptionInfo Default = new AttributeDescriptionInfo(Array.Empty<TagHelperAttributeDescriptionInfo>());

        public AttributeDescriptionInfo(IReadOnlyList<TagHelperAttributeDescriptionInfo> associatedAttributeDescriptions)
        {
            if (associatedAttributeDescriptions == null)
            {
                throw new ArgumentNullException(nameof(associatedAttributeDescriptions));
            }

            AssociatedAttributeDescriptions = associatedAttributeDescriptions;
        }

        public IReadOnlyList<TagHelperAttributeDescriptionInfo> AssociatedAttributeDescriptions { get; }
    }
}
