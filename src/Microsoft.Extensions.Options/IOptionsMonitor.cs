// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.Options
{
    public interface IOptionsMonitor<out TOptions>
    {
        TOptions CurrentValue { get; }
        IDisposable OnChange(Action<TOptions> listener);
    }
}