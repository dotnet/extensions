/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

import { RazorDocumentManager } from '../RazorDocumentManager';
import { RazorLogger } from '../RazorLogger';
import { api } from '../vscodeAdapter';
import * as vscode from '../vscodeAdapter';
import { ReportIssueCreator } from './ReportIssueCreator';
import { ReportIssueDataCollectorFactory } from './ReportIssueDataCollectorFactory';
import { ReportIssuePanel } from './ReportIssuePanel';

export class ReportIssueCommand {
    private readonly issuePanel: ReportIssuePanel;
    private readonly issueCreator: ReportIssueCreator;
    private readonly dataCollectorFactory: ReportIssueDataCollectorFactory;

    constructor(
        private readonly vscodeApi: api,
        documentManager: RazorDocumentManager,
        logger: RazorLogger) {
        this.dataCollectorFactory = new ReportIssueDataCollectorFactory(logger);
        this.issueCreator = new ReportIssueCreator(this.vscodeApi, documentManager);
        this.issuePanel = new ReportIssuePanel(this.dataCollectorFactory, this.issueCreator, logger);
    }

    public register() {
        const registrations: vscode.Disposable[] = [];
        registrations.push(
            this.dataCollectorFactory.register(),
            this.vscodeApi.commands.registerCommand('razor.reportIssue', () => this.issuePanel.show()));
        if (this.vscodeApi.window.registerWebviewPanelSerializer) {
            registrations.push(this.vscodeApi.window.registerWebviewPanelSerializer(ReportIssuePanel.viewType, {
                deserializeWebviewPanel: async (panel: vscode.WebviewPanel) => {
                    await this.issuePanel.revive(panel);
                },
            }));
        }

        return this.vscodeApi.Disposable.from(...registrations);
    }
}
