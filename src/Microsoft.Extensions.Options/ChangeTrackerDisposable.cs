// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.Options
{
    internal class ChangeTrackerDisposable : IDisposable
    {
        public List<IDisposable> Disposables { get; } = new List<IDisposable>();

        public void Dispose()
        {
            foreach (var d in Disposables)
            {
                d?.Dispose();
            }
            Disposables.Clear();
        }
    }
}