// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Razor;

namespace Microsoft.AspNetCore.Razor.OmniSharpPlugin
{
    public abstract class OmniSharpForegroundDispatcher
    {
        internal ForegroundDispatcher InternalDispatcher { get; private protected set; }

        public abstract bool IsForegroundThread { get; }

        public abstract TaskScheduler ForegroundScheduler { get; }

        public abstract TaskScheduler BackgroundScheduler { get; }

        public abstract void AssertForegroundThread([CallerMemberName] string caller = null);

        public abstract void AssertBackgroundThread([CallerMemberName] string caller = null);
    }
}
