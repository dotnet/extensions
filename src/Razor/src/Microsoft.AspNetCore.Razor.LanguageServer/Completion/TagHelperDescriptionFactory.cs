// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Razor.Completion;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Completion
{
    internal abstract class TagHelperDescriptionFactory
    {
        public abstract bool TryCreateDescription(ElementDescriptionInfo descriptionInfos, out MarkupContent markupContent);

        public abstract bool TryCreateDescription(AttributeDescriptionInfo descriptionInfos, out MarkupContent markupContent);

        public abstract bool TryCreateDescription(AttributeCompletionDescription descriptionInfos, out MarkupContent markupContent);
    }
}
