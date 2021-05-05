// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using MediatR;
using Newtonsoft.Json;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Microsoft.AspNetCore.Razor.LanguageServer.CodeActions.Models
{
    [DebuggerDisplay("{Title,nq}")]
    internal class RazorCodeAction : CodeAction, IRequest<RazorCodeAction>, IBaseRequest
    {
        /// <summary>
        /// Typically null, only present in VS scenarios.
        /// </summary>
        [JsonProperty(PropertyName = "children", NullValueHandling = NullValueHandling.Ignore)]
        public RazorCodeAction[] Children { get; set; } = Array.Empty<RazorCodeAction>();

        /// <summary>
        /// Used internally by the Razor Language Server to store the Code Action name extracted
        /// from the Data.CustomTags payload.
        /// </summary>
        [JsonProperty(PropertyName = "name", NullValueHandling = NullValueHandling.Ignore)]
        public string Name { get; set; }
    }
}
