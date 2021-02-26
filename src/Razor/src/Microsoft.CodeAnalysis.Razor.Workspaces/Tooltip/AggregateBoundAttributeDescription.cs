// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Razor.Tooltip
{
    internal class AggregateBoundAttributeDescription
    {
        public static readonly AggregateBoundAttributeDescription Default = new AggregateBoundAttributeDescription(Array.Empty<BoundAttributeDescriptionInfo>());

        public AggregateBoundAttributeDescription(IReadOnlyList<BoundAttributeDescriptionInfo> descriptionInfos)
        {
            if (descriptionInfos == null)
            {
                throw new ArgumentNullException(nameof(descriptionInfos));
            }

            DescriptionInfos = descriptionInfos;
        }

        public IReadOnlyList<BoundAttributeDescriptionInfo> DescriptionInfos { get; }
    }
}
