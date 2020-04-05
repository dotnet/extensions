/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import { HostEventStream, TelemetryEvent } from './HostEventStream';
import { Trace } from './Trace';

export class TelemetryReporter {
    private readonly razorExtensionActivated = new TelemetryEvent('VSCode.Razor.RazorExtensionActivated');
    private readonly debugLanguageServerEvent = new TelemetryEvent('VSCode.Razor.DebugLanguageServer');
    private readonly workspaceContainsRazorEvent = new TelemetryEvent('VSCode.Razor.WorkspaceContainsRazor');
    private reportedWorkspaceContainsRazor = false;

    constructor(
        private readonly eventStream: HostEventStream) {
        // If this telemetry reporter is created it means the rest of the Razor extension world was created.
        this.eventStream.post(this.razorExtensionActivated);
    }

    public reportTraceLevel(trace: Trace) {
        const traceLevelEvent = new TelemetryEvent(
            'VSCode.Razor.TraceLevel',
            {
                trace: Trace[trace],
            });
        this.eventStream.post(traceLevelEvent);
    }

    public reportErrorOnServerStart(error: Error) {
        this.reportError('VSCode.Razor.ErrorOnServerStart', error);
    }

    public reportErrorOnActivation(error: Error) {
        this.reportError('VSCode.Razor.ErrorOnActivation', error);
    }

    public reportDebugLanguageServer() {
        this.eventStream.post(this.debugLanguageServerEvent);
    }

    public reportWorkspaceContainsRazor() {
        if (this.reportedWorkspaceContainsRazor) {
            return;
        }

        this.reportedWorkspaceContainsRazor = true;
        this.eventStream.post(this.workspaceContainsRazorEvent);
    }

    private reportError(eventName: string, error: Error) {
        const errorOnActivationEvent = new TelemetryEvent(
            eventName,
            {
                error: JSON.stringify(error),
            });

        this.eventStream.post(errorOnActivationEvent);
    }
}
