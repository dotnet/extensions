// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics.Tracing;

namespace Microsoft.Framework.TelemetryAdapter
{
    public abstract class TelemetrySourceAdapter : TelemetrySource
    {
        public abstract void EnlistTarget(object listener);
    }
}
