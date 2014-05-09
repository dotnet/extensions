// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Framework.DependencyInjection
{
    public class ServiceDescriptor : IServiceDescriptor
    {
        public LifecycleKind Lifecycle { get; set;  }
        public Type ServiceType { get; set;  }

        // Exactly one of the two following properties should be set
        public Type ImplementationType { get; set; } // nullable
        public object ImplementationInstance { get; set; } // nullable
    }
}
