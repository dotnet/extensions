// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Build.Execution;

namespace Microsoft.AspNetCore.Razor.OmniSharpPlugin
{
    public abstract class ProjectInstanceEvaluator
    {
        public abstract ProjectInstance Evaluate(ProjectInstance projectInstance);
    }
}
