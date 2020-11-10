/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { Trace } from 'vscode-jsonrpc';
import { RazorLogger } from '../RazorLogger';
import { ReportIssueCreator } from './ReportIssueCreator';
import { ReportIssueDataCollector } from './ReportIssueDataCollector';
import { ReportIssueDataCollectorFactory } from './ReportIssueDataCollectorFactory';

export class ReportIssuePanel {
    public static readonly viewType = 'razorReportIssue';

    private panel: vscode.WebviewPanel | undefined;
    private dataCollector: ReportIssueDataCollector | undefined;
    private issueContent: string | undefined;
    private traceLevelChange: vscode.Disposable | undefined;

    constructor(
        private readonly dataCollectorFactory: ReportIssueDataCollectorFactory,
        private readonly reportIssueCreator: ReportIssueCreator,
        private readonly logger: RazorLogger) {
    }

    public async show() {
        if (this.panel) {
            this.panel.reveal(vscode.ViewColumn.Two);
        } else {
            this.panel = vscode.window.createWebviewPanel(
                ReportIssuePanel.viewType,
                'Report Razor Issue',
                vscode.ViewColumn.Two, {
                    enableScripts: true,
                    // Disallow any remote sources
                    localResourceRoots: [],
                });
            this.attachToCurrentPanel();
        }

        await this.update();
    }

    public async revive(panel: vscode.WebviewPanel) {
        this.panel = panel;
        this.attachToCurrentPanel();
        await this.update();
    }

    private attachToCurrentPanel() {
        if (!this.panel) {
            vscode.window.showErrorMessage('Unexpected error when attaching to report Razor issue window.');
            return;
        }

        this.panel.webview.onDidReceiveMessage(async message => {
            switch (message.command) {
                case 'copyIssue':
                    if (!this.issueContent) {
                        if (!this.dataCollector) {
                            vscode.window.showErrorMessage('You must first start the data collection before copying.');
                            return;
                        }
                        const collectionResult = this.dataCollector.collect();
                        this.issueContent = await this.reportIssueCreator.create(collectionResult);
                        this.dataCollector = undefined;
                    }

                    await vscode.env.clipboard.writeText(this.issueContent);
                    vscode.window.showInformationMessage('Razor issue copied to clipboard');
                    return;
                case 'startIssue':
                    if (this.dataCollector) {
                        this.dataCollector.stop();
                        this.dataCollector = undefined;
                    }
                    this.issueContent = undefined;
                    this.dataCollector = this.dataCollectorFactory.create();
                    vscode.window.showInformationMessage('Razor issue data collection started. Reproduce the issue then press "Stop"');
                    return;
                case 'stopIssue':
                    if (!this.dataCollector) {
                        vscode.window.showErrorMessage('You must first start the data collection before stopping.');
                        return;
                    }
                    this.dataCollector.stop();
                    vscode.window.showInformationMessage('Razor issue data collection stopped. Copying issue content...');
                    return;
            }
        });

        this.traceLevelChange = this.logger.onTraceLevelChange(async () => this.update());

        this.panel.onDidDispose(() => {
            if (this.traceLevelChange) {
                this.traceLevelChange.dispose();
            }
            this.panel = undefined;
        });
    }

    private async update() {
        if (!this.panel) {
            return;
        }

        let panelBodyContent = '';
        if (this.logger.trace === Trace.Verbose) {
            panelBodyContent = `<ol>
    <li>Press <button onclick="startIssue()">Start</button></li>
    <li>Perform the actions (or no action) that resulted in your Razor issue</li>
    <li>Click <button onclick="stopIssue()">Stop</button>. This will copy all relevant issue information.</li>
    <li><a href="https://github.com/dotnet/aspnetcore/issues/new?template=bug_report.md&labels=area-razor.tooling%2C+feature-razor.vscode%2C+bug">Go to GitHub</a>, paste your issue contents as the body of the issue. Don't forget to fill out any details left unfilled.</li>
</ol>

<p><em>Privacy Alert! The contents copied to your clipboard may contain personal data. Prior to posting to
GitHub, please remove any personal data which should not be publicly viewable.
<a href="https://privacy.microsoft.com/en-US/privacystatement">https://privacy.microsoft.com/en-US/privacystatement</a></em></p>

<button onclick="copyIssue()">Copy issue content again</button>`;
        } else {
            panelBodyContent = `<p>Cannot start collecting Razor logs when <strong><em>razor.trace</em></strong> is set to <strong><em>${Trace[this.logger.trace]}</em></strong>.
Please set <strong><em>razor.trace</em></strong> to <strong><em>${Trace[Trace.Verbose]}</em></strong> and then reload your VSCode environment and re-run the report Razor issue command.</p>`;
        }

        this.panel.webview.html = `<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta http-equiv="Content-Security-Policy">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Report a Razor issue</title>
    <style>
        button {
            background-color: #eff3f6;
            background-image: linear-gradient(-180deg,#fafbfc,#eff3f6 90%);
            color: #24292e;
            border: 1px solid rgba(27,31,35,.2);
            border-radius: .25em;
            font-size: 1.1em;
            font-weight: 600;
            line-height: 18px;
            padding: 6px 12px;
            vertical-align: middle;
            cursor: pointer;
        }
        body {
            font-size: 20px;
        }
        strong {
            color: red;
        }
    </style>

    <script type="text/javascript">
        const vscode = acquireVsCodeApi();
        function copyIssue() {
            vscode.postMessage({
                command: 'copyIssue'
            });
        }
        function startIssue() {
            vscode.postMessage({
                command: 'startIssue'
            });
        }
        function stopIssue() {
            vscode.postMessage({
                command: 'stopIssue'
            });
            copyIssue();
        }
    </script>
</head>
<body>
<h3>Report a Razor issue</h3>
${panelBodyContent}
</body>
</html>`;
    }
}
