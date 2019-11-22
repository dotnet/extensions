// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using OmniSharp.Extensions.Embedded.MediatR;

namespace Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem
{
    internal class RazorRemoveProjectParams : IRequest
    {
        public string FilePath { get; set; }
    }
}
