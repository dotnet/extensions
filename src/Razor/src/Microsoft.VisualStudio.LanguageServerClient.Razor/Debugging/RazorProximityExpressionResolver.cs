// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.Debugging
{
    internal abstract class RazorProximityExpressionResolver
    {
        public abstract Task<IReadOnlyList<string>> TryResolveProximityExpressionsAsync(ITextBuffer textBuffer, int lineIndex, int characterIndex, CancellationToken cancellationToken);
    }
}
