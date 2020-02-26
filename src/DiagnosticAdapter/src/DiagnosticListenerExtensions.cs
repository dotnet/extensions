// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
