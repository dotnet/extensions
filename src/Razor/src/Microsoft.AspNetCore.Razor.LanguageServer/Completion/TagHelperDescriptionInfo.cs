// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Completion
{
    internal class TagHelperDescriptionInfo
    {
        public TagHelperDescriptionInfo(string tagHelperTypeName, string documentation)
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
    }
}
