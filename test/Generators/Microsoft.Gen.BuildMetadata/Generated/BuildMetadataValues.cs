// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Gen.BuildMetadata;

// This attribute is used to control the generation of the BuildMetadata
[assembly: GenerateBuildMetadata(
    "TEST_BUILDID",
    "TEST_BUILDNUMBER",
    "TEST_SOURCEBRANCHNAME",
    "TEST_SOURCEVERSION")]

namespace Microsoft.Gen.BuildMetadata.Test;

internal static class BuildMetadataValues
{
    public const string BuildId = "TEST_BUILDID";
    public const string BuildNumber = "TEST_BUILDNUMBER";
    public const string SourceBranchName = "TEST_SOURCEBRANCHNAME";
    public const string SourceVersion = "TEST_SOURCEVERSION";
}
