/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { RazorLanguage } from '../RazorLanguage';
import { RazorLogger } from '../RazorLogger';
import { ReportIssueDataCollector } from './ReportIssueDataCollector';

export class ReportIssueDataCollectorFactory {
    private onRazorDocumentFocusedEmitter = new vscode.EventEmitter<vscode.TextDocument>();

    constructor(private readonly logger: RazorLogger) {
        this.onRazorDocumentFocusedEmitter = new vscode.EventEmitter<vscode.TextDocument>();
    }

    public register() {
        return vscode.window.onDidChangeActiveTextEditor((newEditor) => {
            if (newEditor && RazorLanguage.fileExtensions.some(ext => newEditor.document.fileName.endsWith(ext))) {
                this.onRazorDocumentFocusedEmitter.fire(newEditor.document);
            }
        });
    }

    public create() {
        const collector = new ReportIssueDataCollector(this.onRazorDocumentFocusedEmitter.event, this.logger);
        return collector;
    }
}
