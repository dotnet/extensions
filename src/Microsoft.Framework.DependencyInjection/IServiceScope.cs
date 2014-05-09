// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
//using Microsoft.Net.Runtime;

namespace Microsoft.Framework.DependencyInjection
{
    /// <summary>
    /// The <see cref="System.IDisposable.Dispose"/> method ends the scope lifetime. Once Dispose
    /// is called, any scoped services that have been resolved from
    /// <see cref="Microsoft.Framework.DependencyInjection.IServiceScope.ServiceProvider"/> will be
    /// disposed.
    /// </summary>
    //[AssemblyNeutral]
    public interface IServiceScope : IDisposable
    {
        /// <summary>
        /// The <see cref="System.IServiceProvider"/> used to resolve dependencies from the scope.
        /// </summary>
        IServiceProvider ServiceProvider { get; }
    }
}
