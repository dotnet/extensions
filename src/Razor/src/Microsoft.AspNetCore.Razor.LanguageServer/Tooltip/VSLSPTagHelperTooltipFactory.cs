// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Razor.Tooltip;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Tooltip
{
    internal abstract class VSLSPTagHelperTooltipFactory : TagHelperTooltipFactoryBase
    {
        public abstract bool TryCreateTooltip(AggregateBoundElementDescription descriptionInfos, out VSClassifiedTextElement tooltipContent);

        public abstract bool TryCreateTooltip(AggregateBoundAttributeDescription descriptionInfos, out VSClassifiedTextElement tooltipContent);
    }
}
