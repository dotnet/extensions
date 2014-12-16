// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Testing.xunit
{
    [Flags]
    public enum RuntimeFrameworks
    {
        None = 0,
        Mono = 1 << 0,
        Dotnet = 1 << 1
    }
}