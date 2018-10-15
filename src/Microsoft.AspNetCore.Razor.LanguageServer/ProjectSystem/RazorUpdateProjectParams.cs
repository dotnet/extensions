// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language;
using OmniSharp.Extensions.Embedded.MediatR;

namespace Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem
{
    internal class RazorUpdateProjectParams : IRequest
    {
        public string ProjectFilePath { get; set; }

        public string TargetFramework { get; set; }

        public IReadOnlyList<TagHelperDescriptor> TagHelpers { get; set; }

        public RazorConfiguration Configuration { get; set; }
    }
}
