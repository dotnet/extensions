// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Borrowed from https://github.com/dotnet/aspnetcore/blob/95ed45c67/src/Testing/src/xunit/

using System;

namespace Microsoft.TestUtilities;

[Flags]
public enum OperatingSystems
{
    Linux = 1,
    MacOSX = 2,
    Windows = 4,
}
