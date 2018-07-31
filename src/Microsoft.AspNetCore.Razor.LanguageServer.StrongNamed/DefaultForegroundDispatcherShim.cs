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
        private readonly ForegroundDispatcher _foregroundDispatcher;

        public DefaultForegroundDispatcherShim(ForegroundDispatcher foregroundDispatcher)
        {
            if (foregroundDispatcher == null)
            {
                throw new ArgumentNullException(nameof(foregroundDispatcher));
            }

            _foregroundDispatcher = foregroundDispatcher;
        }

        public override bool IsForegroundThread => _foregroundDispatcher.IsForegroundThread;

        public override TaskScheduler ForegroundScheduler => _foregroundDispatcher.ForegroundScheduler;

        public override TaskScheduler BackgroundScheduler => _foregroundDispatcher.BackgroundScheduler;

        public override void AssertForegroundThread([CallerMemberName] string caller = null) => _foregroundDispatcher.AssertForegroundThread(caller);

        public override void AssertBackgroundThread([CallerMemberName] string caller = null) => _foregroundDispatcher.AssertBackgroundThread(caller);
    }
}
