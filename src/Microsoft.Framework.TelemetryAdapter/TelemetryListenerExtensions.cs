// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.TelemetryAdapter;

namespace System.Diagnostics.Tracing
{
    public static class TelemetryListenerExtensions
    {
        public static IDisposable SubscribeWithAdapter(this TelemetryListener telemetry, object target)
        {
#if PROXY_SUPPORT
            var adapter = new TelemetrySourceAdapter(new ProxyTelemetrySourceMethodAdapter());
#else
            var adapter = new TelemetrySourceAdapter(new ReflectionTelemetrySourceMethodAdapter());
#endif
            adapter.EnlistTarget(target);
            return telemetry.Subscribe(adapter, adapter.IsEnabled);
        }
    }
}
