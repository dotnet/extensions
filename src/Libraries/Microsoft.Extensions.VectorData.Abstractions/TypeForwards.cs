// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET
using System.Runtime.CompilerServices;

// A library compiled for .NET Standard calls the init setters of this package with the IsExternalInit of the .NET Standard build,
// and at run time gets the .NET build, whose setters have the one of the runtime: this makes the two the same type.
[assembly: TypeForwardedTo(typeof(IsExternalInit))]
#endif
