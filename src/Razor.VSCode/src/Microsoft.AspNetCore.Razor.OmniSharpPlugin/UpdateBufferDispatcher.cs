// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using OmniSharp.Models;

namespace Microsoft.AspNetCore.Razor.OmniSharpPlugin
{
    public abstract class UpdateBufferDispatcher
    {
        public abstract Task UpdateBufferAsync(Request request);
    }
}
