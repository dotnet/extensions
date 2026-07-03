// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// isLeafFailed must stay identical to the tree's aggregate pass/fail so KPI and Cases counts can't diverge.
// It lives in this dependency-free module so both Summary and viewModels can use it without importing each
// other (breaks the former Summary<->viewModels runtime import cycle).
export const isLeafFailed = (scenario: ScenarioRunResult): boolean =>
    Object.values(scenario.evaluationResult?.metrics ?? {}).some(
        (metric) =>
            metric?.interpretation?.failed === true ||
            (metric?.diagnostics?.some((d) => d.severity === 'error') ?? false),
    );
