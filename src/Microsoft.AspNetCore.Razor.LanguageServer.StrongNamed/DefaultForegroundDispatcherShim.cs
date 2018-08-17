// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Razor;

namespace Microsoft.AspNetCore.Razor.LanguageServer.StrongNamed
{
    internal class DefaultForegroundDispatcherShim : ForegroundDispatcherShim
    {
        public DefaultForegroundDispatcherShim(ForegroundDispatcher foregroundDispatcher)
        {
            if (foregroundDispatcher == null)
            {
                throw new ArgumentNullException(nameof(foregroundDispatcher));
            }

            InnerForegroundDispatcher = foregroundDispatcher;
        }

        public ForegroundDispatcher InnerForegroundDispatcher { get; }

        public override bool IsForegroundThread => InnerForegroundDispatcher.IsForegroundThread;

        public override TaskScheduler ForegroundScheduler => InnerForegroundDispatcher.ForegroundScheduler;

        public override TaskScheduler BackgroundScheduler => InnerForegroundDispatcher.BackgroundScheduler;

        public override void AssertForegroundThread([CallerMemberName] string caller = null) => InnerForegroundDispatcher.AssertForegroundThread(caller);

        public override void AssertBackgroundThread([CallerMemberName] string caller = null) => InnerForegroundDispatcher.AssertBackgroundThread(caller);
    }
}
