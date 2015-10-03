// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq.Expressions;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    /// <summary>
    /// Summary description for IServiceCallSite
    /// </summary>
    internal interface IServiceCallSite
    {
        object Invoke(ServiceProvider provider);

        Expression Build(Expression provider);
    }
}