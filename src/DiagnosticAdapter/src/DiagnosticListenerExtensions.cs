// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.DiagnosticAdapter;

namespace System.Diagnostics
{
    public static class DiagnosticListenerExtensions
    {
        public static IDisposable SubscribeWithAdapter(this DiagnosticListener diagnostic, object target)
        {
            var adapter = new DiagnosticSourceAdapter(target);
            return diagnostic.Subscribe(adapter, (Predicate<string>)adapter.IsEnabled);
        }

        public static IDisposable SubscribeWithAdapter(
            this DiagnosticListener diagnostic,
            object target,
            Func<string, bool> isEnabled)
        {
            var adapter = new DiagnosticSourceAdapter(target, isEnabled);
            return diagnostic.Subscribe(adapter, (Predicate<string>)adapter.IsEnabled);
        }

        public static IDisposable SubscribeWithAdapter(
            this DiagnosticListener diagnostic,
            object target,
            Func<string, object, object, bool> isEnabled)
        {
            var adapter = new DiagnosticSourceAdapter(target, isEnabled);
            return diagnostic.Subscribe(adapter, (Func<string, object, object, bool>)adapter.IsEnabled);
        }
    }
}
