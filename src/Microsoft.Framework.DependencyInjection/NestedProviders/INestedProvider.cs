// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Framework.DependencyInjection
{
    public interface INestedProvider<T>
    {
        int Order { get; }

        void Invoke(T context, Action callNext);
    }
}
