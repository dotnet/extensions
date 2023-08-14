// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Gen.EnumStrings.Model;

[ExcludeFromCodeCoverage]
internal sealed record class ToStringMethod(
    string EnumTypeName,
    List<string> MemberNames,
    List<ulong> MemberValues,
    bool FlagsEnum,
    string ExtensionNamespace,
    string ExtensionClass,
    string ExtensionMethod,
    string ExtensionClassModifiers,
    string UnderlyingType);
