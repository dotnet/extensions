// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Common
{
    public abstract class RemoteTextLoaderFactory
    {
        public abstract TextLoader Create(string filePath);
    }
}
