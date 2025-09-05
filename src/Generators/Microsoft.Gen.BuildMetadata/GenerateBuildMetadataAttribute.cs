// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Gen.BuildMetadata;

/// <summary>
/// Attribute used to verify build metadata in tests.
/// </summary>
/// <remarks>
/// It must be public for Roslyn SDK to be able to access it.
/// </remarks>
[ExcludeFromCodeCoverage]
[EditorBrowsable(EditorBrowsableState.Never)]
[AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = false)]
public sealed class GenerateBuildMetadataAttribute : Attribute
{
#pragma warning disable IDE0060 // Remove unused parameter
#pragma warning disable CA1019 // Define accessors for attribute arguments
    public GenerateBuildMetadataAttribute(string buildId, string buildNumber, string sourceBranchName, string sourceVersion, int buildDateTime)
    {
    }
#pragma warning restore CA1019 // Define accessors for attribute arguments
#pragma warning restore IDE0060 // Remove unused parameter
}
