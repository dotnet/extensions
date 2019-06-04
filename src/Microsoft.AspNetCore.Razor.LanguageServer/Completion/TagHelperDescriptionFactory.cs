// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Razor.Completion;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Completion
{
    internal abstract class TagHelperDescriptionFactory
    {
        public abstract bool TryCreateDescription(ElementDescriptionInfo descriptionInfos, out string markdown);

        public abstract bool TryCreateDescription(AttributeDescriptionInfo descriptionInfos, out string markdown);

        public abstract bool TryCreateDescription(AttributeCompletionDescription descriptionInfos, out string markdown);
    }
}
