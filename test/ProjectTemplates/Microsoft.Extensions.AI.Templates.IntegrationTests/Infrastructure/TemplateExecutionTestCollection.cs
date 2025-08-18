// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.Extensions.AI.Templates.Tests;

[CollectionDefinition(name: Name)]
public sealed class TemplateExecutionTestCollection : ICollectionFixture<TemplateExecutionTestCollectionFixture>
{
    public const string Name = "Template execution test";
}
