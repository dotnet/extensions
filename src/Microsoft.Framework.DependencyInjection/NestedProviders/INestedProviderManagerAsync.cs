// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.Framework.DependencyInjection
{
    public interface INestedProviderManagerAsync<T>
    {
        Task InvokeAsync(T context);
    }
}
