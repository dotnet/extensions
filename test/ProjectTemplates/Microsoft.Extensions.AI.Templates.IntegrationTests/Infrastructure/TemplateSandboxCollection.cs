// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.Extensions.AI.Templates.Tests.Infrastructure;

[CollectionDefinition("Template sandbox")]
public sealed class TemplateSandboxCollection : ICollectionFixture<TemplateSandboxFixture>
{
}
