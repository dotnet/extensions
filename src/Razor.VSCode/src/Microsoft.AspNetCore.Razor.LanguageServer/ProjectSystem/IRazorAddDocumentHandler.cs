// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using OmniSharp.Extensions.JsonRpc;

namespace Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem
{
    [Parallel, Method("projects/addDocument")]
    internal interface IRazorAddDocumentHandler : IJsonRpcRequestHandler<AddDocumentParams>
    {
    }
}
