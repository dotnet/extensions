// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.Framework.DependencyInjection
{
    /// <summary>
    /// Specifies the contract for a collection of service descriptors.
    /// </summary>
#if ASPNET50 || ASPNETCORE50
    [Microsoft.Framework.Runtime.AssemblyNeutral]
#endif
    public interface IServiceCollection : ICollection<IServiceDescriptor>
    {
        /// <summary>
        /// Adds the <paramref name="descriptor"/> to this instance.
        /// </summary>
        /// <param name="descriptor">The <see cref="IServiceDescriptor"/> to add.</param>
        /// <returns>A reference to the current instance of <see cref="IServiceCollection"/>.</returns>
        new IServiceCollection Add(IServiceDescriptor descriptor);
    }
}
