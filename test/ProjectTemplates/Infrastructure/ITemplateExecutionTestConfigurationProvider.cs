// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Shared.ProjectTemplates.Tests;

public interface ITemplateExecutionTestConfigurationProvider
{
    static abstract TemplateExecutionTestConfiguration Configuration { get; }
}
