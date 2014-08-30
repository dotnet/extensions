// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Framework.DependencyInjection
{
    /// <summary>
    /// Secifies the contract for a type that contains information about a service.
    /// </summary>
    /// <remarks>
    /// <see cref="ImplementationType"/>, <see cref="ImplementationInstance"/>, amd <see cref="ImplementationFactory"/> specify the source 
    /// for the service instance. Only one of them should ever be non-null for a given <see cref="IServiceDescriptor"/> instance.
    /// </remarks>
#if ASPNET50 || ASPNETCORE50
    [Microsoft.Framework.Runtime.AssemblyNeutral]
#endif
    public interface IServiceDescriptor
    {
        /// <summary>
        /// Gets the <see cref="LifecycleKind"/> of the service.
        /// </summary>
        LifecycleKind Lifecycle { get; }

        /// <summary>
        /// Gets the <see cref="Type"/> of the service.
        /// </summary>
        Type ServiceType { get; }

        /// <summary>
        /// Gets the <see cref="ImplementationType"/> of the service.
        /// </summary>
        Type ImplementationType { get; }

        /// <summary>
        /// Gets the instance implementing the service.
        /// </summary>
        object ImplementationInstance { get; }

        /// <summary>
        /// Gets the factory used for creating service instances.
        /// </summary>
        Func<IServiceProvider, object> ImplementationFactory { get; }
    }
}
