// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;

namespace Microsoft.AspNetCore.Razor.OmniSharpPlugin
{
    // This class enables us to re-evaluate MSBuild project instances. Doing such a thing isn't directly
    // supported by the ProjectInstance type because they're meant to be a snapshot of an MSBuild project
    // at a certain point in time. Therefore, we re-create the MSBuild project environment by lifting all
    // configuration values from the project instance and re-creating our own MSBuild project collection
    // that can duplicate the work that OmniSharp does.
    //
    // Project re-evaluation is required for two reasons:
    //  1. Razor file additions/deletions. OmniSharp tells us when a change happens to impact the csproj
    //     configuration but not the opaque items of a project such as content/initial compile items.
    //  2. When OmniSharp initially evaluates the users project they don't do a true design time build.
    //     Because of this several bits of Razor information get left out in the initial project instance,
    //     we then need to force the execution of the appropriate targets to populate that information on
    //     the project instance.
    [Shared]
    [Export(typeof(ProjectInstanceEvaluator))]
    public class DefaultProjectInstanceEvaluator : ProjectInstanceEvaluator
    {
        internal const string TargetFrameworkPropertyName = "TargetFramework";
        internal const string TargetFrameworksPropertyName = "TargetFrameworks";
        private const string CompileTargetName = "Compile";
        private const string CoreCompileTargetName = "CoreCompile";
        private const string RazorGenerateDesignTimeTargetName = "RazorGenerateDesignTime";
        private const string RazorGenerateComponentDesignTimeTargetName = "RazorGenerateComponentDesignTime";
        private static readonly IEnumerable<ILogger> EmptyMSBuildLoggers = Enumerable.Empty<ILogger>();
        private readonly OmniSharpForegroundDispatcher _foregroundDispatcher;
        private readonly object _evaluationLock = new object();

        [ImportingConstructor]
        public DefaultProjectInstanceEvaluator(OmniSharpForegroundDispatcher foregroundDispatcher)
        {
            if (foregroundDispatcher == null)
            {
                throw new ArgumentNullException(nameof(foregroundDispatcher));
            }

            _foregroundDispatcher = foregroundDispatcher;
        }

        public override ProjectInstance Evaluate(ProjectInstance projectInstance)
        {
            if (projectInstance == null)
            {
                throw new ArgumentNullException(nameof(projectInstance));
            }

            lock (_evaluationLock)
            {
                _foregroundDispatcher.AssertBackgroundThread();

                var refreshTargets = new List<string>()
                {
                    // These are the default targets for the project instance that OmniSharp runs.
                    CompileTargetName,
                    CoreCompileTargetName
                };

                if (projectInstance.Targets.ContainsKey(RazorGenerateDesignTimeTargetName))
                {
                    refreshTargets.Add(RazorGenerateDesignTimeTargetName);
                }

                if (projectInstance.Targets.ContainsKey(RazorGenerateComponentDesignTimeTargetName))
                {
                    refreshTargets.Add(RazorGenerateComponentDesignTimeTargetName);
                }

                if (refreshTargets.Count > 2)
                {
                    var _projectCollection = new ProjectCollection(projectInstance.GlobalProperties);
                    var project = _projectCollection.LoadProject(projectInstance.ProjectFileLocation.File, projectInstance.ToolsVersion);
                    SetTargetFrameworkIfNeeded(project);

                    var refreshedProjectInstance = project.CreateProjectInstance();

                    // Force a Razor information refresh
                    refreshedProjectInstance.Build(refreshTargets.ToArray(), EmptyMSBuildLoggers);

                    return refreshedProjectInstance;
                }

                return projectInstance;
            }
        }

        private static void SetTargetFrameworkIfNeeded(Project evaluatedProject)
        {
            var targetFramework = evaluatedProject.GetPropertyValue(TargetFrameworkPropertyName);
            var targetFrameworksRaw = evaluatedProject.GetPropertyValue(TargetFrameworksPropertyName);
            var targetFrameworks = targetFrameworksRaw
                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(framework => framework.Trim())
                .ToArray();

            if (string.IsNullOrWhiteSpace(targetFramework) && targetFrameworks.Length > 0)
            {
                // Pick first target framework to replicate what OmniSharp does.
                targetFramework = targetFrameworks[0];
                evaluatedProject.SetProperty(TargetFrameworkPropertyName, targetFramework);
                evaluatedProject.ReevaluateIfNecessary();
            }
        }
    }
}
