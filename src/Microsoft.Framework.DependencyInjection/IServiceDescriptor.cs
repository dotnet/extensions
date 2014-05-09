// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
//using Microsoft.Net.Runtime;

namespace Microsoft.Framework.DependencyInjection
{
    //[AssemblyNeutral]
    public interface IServiceDescriptor
    {
        LifecycleKind Lifecycle { get; }
        Type ServiceType { get; }
        Type ImplementationType { get; } // nullable
        object ImplementationInstance { get; } // nullable
    }
}
