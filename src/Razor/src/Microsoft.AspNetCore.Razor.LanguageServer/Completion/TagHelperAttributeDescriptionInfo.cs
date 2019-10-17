// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Completion
{
    internal class TagHelperAttributeDescriptionInfo
    {
        public TagHelperAttributeDescriptionInfo(
            string displayName,
            string propertyName,
            string returnTypeName,
            string documentation)
        {
            if (displayName == null)
            {
                throw new ArgumentNullException(nameof(displayName));
            }

            if (propertyName == null)
            {
                throw new ArgumentNullException(nameof(propertyName));
            }

            if (returnTypeName == null)
            {
                throw new ArgumentNullException(nameof(returnTypeName));
            }

            DisplayName = displayName;
            PropertyName = propertyName;
            ReturnTypeName = returnTypeName;
            Documentation = documentation;
        }

        public string DisplayName { get; }

        public string PropertyName { get; }

        public string ReturnTypeName { get; }

        public string Documentation { get; }
    }
}
