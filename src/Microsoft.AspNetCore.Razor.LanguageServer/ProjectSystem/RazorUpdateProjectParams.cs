// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using OmniSharp.Extensions.Embedded.MediatR;

namespace Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem
{
    internal class RazorUpdateProjectParams : IRequest
    {
        public string FilePath { get; set; }

        public ProjectWorkspaceState ProjectWorkspaceState { get; set; }

        public RazorConfiguration Configuration { get; set; }
    }
}
