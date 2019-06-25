// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.LanguageServer.Common.Serialization;
using OmniSharp.Extensions.Embedded.MediatR;

namespace Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem
{
    internal class RazorUpdateProjectParams : IRequest
    {
        public FullProjectSnapshotHandle ProjectSnapshotHandle { get; set; }
    }
}
