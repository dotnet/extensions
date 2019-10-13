// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using OmniSharp;
using OmniSharp.Models;
using OmniSharp.Roslyn;

namespace Microsoft.AspNetCore.Razor.OmniSharpPlugin
{
    [Shared]
    [Export(typeof(UpdateBufferDispatcher))]
    internal class DefaultUpdateBufferDispatcher : UpdateBufferDispatcher
    {
        private readonly BufferManager _bufferManager;
        private readonly SemaphoreSlim _updateLock;

        [ImportingConstructor]
        public DefaultUpdateBufferDispatcher(OmniSharpWorkspace omniSharpWorkspace)
        {
            if (omniSharpWorkspace == null)
            {
                throw new ArgumentNullException(nameof(omniSharpWorkspace));
            }

            _bufferManager = omniSharpWorkspace.BufferManager;
            _updateLock = new SemaphoreSlim(1);
        }

        public override async Task UpdateBufferAsync(Request request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            await _updateLock.WaitAsync();

            await _bufferManager.UpdateBufferAsync(request);

            _updateLock.Release();
        }
    }
}
