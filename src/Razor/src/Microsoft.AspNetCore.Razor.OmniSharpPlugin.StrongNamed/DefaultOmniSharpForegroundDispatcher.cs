// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;

namespace Microsoft.AspNetCore.Razor.OmniSharpPlugin
{
    public class DefaultOmniSharpForegroundDispatcher : OmniSharpForegroundDispatcher
    {
        public DefaultOmniSharpForegroundDispatcher()
        {
            InternalDispatcher = new VSCodeForegroundDispatcher();
        }

        public override bool IsForegroundThread => InternalDispatcher.IsForegroundThread;
        public override TaskScheduler ForegroundScheduler => InternalDispatcher.ForegroundScheduler;
        public override TaskScheduler BackgroundScheduler => InternalDispatcher.BackgroundScheduler;

        public override void AssertBackgroundThread([CallerMemberName] string caller = null) => InternalDispatcher.AssertBackgroundThread(caller);
        public override void AssertForegroundThread([CallerMemberName] string caller = null) => InternalDispatcher.AssertForegroundThread(caller);
    }
}
