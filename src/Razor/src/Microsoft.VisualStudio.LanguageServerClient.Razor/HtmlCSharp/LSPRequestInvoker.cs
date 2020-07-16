// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.HtmlCSharp
{
    internal abstract class LSPRequestInvoker
    {
        public abstract Task<TOut> ReinvokeRequestOnServerAsync<TIn, TOut>(
            string method,
            LanguageServerKind serverKind,
            TIn parameters,
            CancellationToken cancellationToken);

        public abstract Task<TOut> CustomRequestServerAsync<TIn, TOut>(
            string method,
            LanguageServerKind serverKind,
            TIn parameters,
            CancellationToken cancellationToken);
    }
}
