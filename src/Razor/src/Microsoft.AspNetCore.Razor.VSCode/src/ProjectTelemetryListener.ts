/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import { RazorProjectChangeKind } from './RazorProjectChangeKind';
import { RazorProjectManager } from './RazorProjectManager';
import { TelemetryReporter } from './TelemetryReporter';

export function reportTelemetryForProjects(
    projectManager: RazorProjectManager,
    telemetryReporter: TelemetryReporter) {
    projectManager.onChange((event) => {
        switch (event.kind) {
            case RazorProjectChangeKind.changed:
                telemetryReporter.reportProjectInfo(event.project);
                break;
        }
    });
}
