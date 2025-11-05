// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Shared.ProjectTemplates.Tests;
using Xunit;

namespace Microsoft.Shared.ProjectTemplates.Tests;

[CollectionDefinition(name: Name)]
public sealed class TemplateExecutionTestCollection : ICollectionFixture<TemplateExecutionTestCollectionFixture>
{
    public const string Name = "Template execution test";
}
