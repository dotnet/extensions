// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

using System;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.CodeAnalysis.Razor.Tooltip
{
    internal class BoundElementDescriptionInfo
    {
        public BoundElementDescriptionInfo(string tagHelperTypeName, string documentation)
        {
            if (tagHelperTypeName == null)
            {
                throw new ArgumentNullException(nameof(tagHelperTypeName));
            }

            TagHelperTypeName = tagHelperTypeName;
            Documentation = documentation;
        }

        public string TagHelperTypeName { get; }

        public string Documentation { get; }

        public static BoundElementDescriptionInfo From(TagHelperDescriptor tagHelper)
        {
            var tagHelperTypeName = tagHelper.GetTypeName();
            var descriptionInfo = new BoundElementDescriptionInfo(tagHelperTypeName, tagHelper.Documentation);
            return descriptionInfo;
        }
    }

}
