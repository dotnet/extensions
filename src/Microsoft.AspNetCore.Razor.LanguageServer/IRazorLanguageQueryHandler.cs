// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using OmniSharp.Extensions.JsonRpc;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    [Parallel, Method("razor/languageQuery")]
    internal interface IRazorLanguageQueryHandler : IJsonRpcRequestHandler<RazorLanguageQueryParams, RazorLanguageQueryResponse>
    {
    }
}
