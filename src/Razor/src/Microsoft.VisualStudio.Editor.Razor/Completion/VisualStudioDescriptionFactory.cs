// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Razor.Tooltip;
using Microsoft.VisualStudio.Text.Adornments;

namespace Microsoft.VisualStudio.Editor.Razor.Completion
{
    internal abstract class VisualStudioDescriptionFactory
    {
        public abstract ContainerElement CreateClassifiedDescription(AggregateBoundAttributeDescription completionDescription);
    }
}
