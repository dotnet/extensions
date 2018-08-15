// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using MediatR;

namespace Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem
{
    public class AddDocumentParams : IRequest
    {
        public string FilePath { get; set; }
    }
}
