/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as os from 'os';
import * as vscode from 'vscode';
import { RazorLogger } from '../RazorLogger';
import { IReportIssueDataCollectionResult } from './IReportIssueDataCollectionResult';

export class ReportIssueDataCollector {
    private readonly logMessages: string[] = [];
    private logOutput = '';
    private focusRegistration: vscode.Disposable | undefined;
    private logRegistration: vscode.Disposable | undefined;
    private lastFocusedRazorDocument: vscode.TextDocument | undefined;
    constructor(
        private readonly razorFileFocusChange: vscode.Event<vscode.TextDocument>,
        private readonly logger: RazorLogger) {

        this.focusRegistration = this.razorFileFocusChange((razorDocument) => this.lastFocusedRazorDocument = razorDocument);
        this.logRegistration = this.logger.onLog(message => this.logMessages.push(message));

        this.logger.outputChannel.show(/* preserveFocus: */ true);
        this.logger.logAlways('-- Starting Issue Data Collection-- ');
    }

    public stop() {
        this.logger.logAlways('-- Stopping Issue Data Collection-- ');
        this.logOutput = this.logMessages.join(os.EOL);
        this.logMessages.length = 0;
        if (this.focusRegistration) {
            this.focusRegistration.dispose();
        }
        if (this.logRegistration) {
            this.logRegistration.dispose();
        }
    }

    public collect(): IReportIssueDataCollectionResult {
        return {
            document: this.lastFocusedRazorDocument,
            logOutput: this.logOutput,
        };
    }
}
