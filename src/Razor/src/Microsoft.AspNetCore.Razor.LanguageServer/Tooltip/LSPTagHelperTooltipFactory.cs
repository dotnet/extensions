// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Razor.Tooltip;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Tooltip
{
    internal abstract class LSPTagHelperTooltipFactory : TagHelperTooltipFactoryBase
    {
        public abstract bool TryCreateTooltip(AggregateBoundElementDescription descriptionInfos, out MarkupContent tooltipContent);

        public abstract bool TryCreateTooltip(AggregateBoundAttributeDescription descriptionInfos, out MarkupContent tooltipContent);
    }
}
