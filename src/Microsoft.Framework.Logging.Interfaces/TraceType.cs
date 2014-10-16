// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Framework.Logging
{
#if ASPNET50 || ASPNETCORE50
    [Runtime.AssemblyNeutral]
#endif
    public enum TraceType
    {
        Verbose = 1,
        Information = 2,
        Warning = 3,
        Error = 4,
        Critical = 5,
    }
}
