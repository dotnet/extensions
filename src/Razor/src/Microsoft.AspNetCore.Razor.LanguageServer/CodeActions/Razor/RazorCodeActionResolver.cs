// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Microsoft.AspNetCore.Razor.LanguageServer.CodeActions
{
    internal abstract class RazorCodeActionResolver : BaseCodeActionResolver
    {
        public abstract Task<WorkspaceEdit> ResolveAsync(JObject data, CancellationToken cancellationToken);
    }
}
