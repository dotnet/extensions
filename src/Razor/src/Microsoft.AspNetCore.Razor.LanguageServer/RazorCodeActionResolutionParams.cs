// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using MediatR;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal class RazorCodeActionResolutionParams : IRequest<RazorCodeActionResolutionResponse>
    {
        public string Action { get; set; }
        public JObject Data { get; set; }
    }
}
