// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.AI.Evaluation.Console.Telemetry;

// Note: The below code is based on the code in the following file in the dotnet CLI:
// https://github.com/dotnet/sdk/blob/main/src/Cli/dotnet/Telemetry/CIEnvironmentDetectorForTelemetry.cs.
//
// The logic below should be kept in sync with the code linked above.

internal static class EnvironmentHelper
{
    internal static bool GetEnvironmentVariableAsBoolean(string name) =>
        Environment.GetEnvironmentVariable(name)?.ToUpperInvariant() switch
        {
            "TRUE" or "1" or "YES" => true,
            _ => false
        };

    // CI systems that can be detected via an environment variable with boolean value true.
    private static readonly string[] _mustBeTrueCIVariables =
        [
            "CI",             // A general-use flag supported by many of the major CI systems including: Azure DevOps, GitHub, GitLab, AppVeyor, Travis CI, CircleCI.
            "TF_BUILD",       // https://docs.microsoft.com/en-us/azure/devops/pipelines/build/variables#system-variables-devops-services
            "GITHUB_ACTIONS", // https://docs.github.com/en/actions/reference/workflows-and-actions/variables
            "APPVEYOR",       // https://www.appveyor.com/docs/environment-variables/
            "TRAVIS",         // https://docs.travis-ci.com/user/environment-variables/#default-environment-variables
            "CIRCLECI",       // https://circleci.com/docs/reference/variables/#built-in-environment-variables
        ];

    // CI systems that that can be detected via a set of environment variables where every variable must be present and
    // must have a non-null value.
    private static readonly string[][] _mustNotBeNullCIVariables =
        [
            ["CODEBUILD_BUILD_ID", "AWS_REGION"], // https://docs.aws.amazon.com/codebuild/latest/userguide/build-env-ref-env-vars.html
            ["BUILD_ID", "BUILD_URL"],            // https://github.com/jenkinsci/jenkins/blob/master/core/src/main/resources/jenkins/model/CoreEnvironmentContributor/buildEnv.groovy
            ["BUILD_ID", "PROJECT_ID"],           // https://cloud.google.com/build/docs/configuring-builds/substitute-variable-values#using_default_substitutions
            ["TEAMCITY_VERSION"],                 // https://www.jetbrains.com/help/teamcity/predefined-build-parameters.html#Predefined+Server+Build+Parameters
            ["JB_SPACE_API_URL"]                  // https://www.jetbrains.com/help/space/automation-parameters.html#provided-parameters
        ];

    public static bool IsCIEnvironment()
    {
        foreach (string variable in _mustBeTrueCIVariables)
        {
            if (bool.TryParse(Environment.GetEnvironmentVariable(variable), out bool value) && value)
            {
                return true;
            }
        }

        foreach (string[] variables in _mustNotBeNullCIVariables)
        {
            if (Array.TrueForAll(variables, static variable => !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(variable))))
            {
                return true;
            }
        }

        return false;
    }
}
