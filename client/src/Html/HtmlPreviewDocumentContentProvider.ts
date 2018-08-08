/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { HtmlProjectedDocument } from './HtmlProjectedDocument';
import { HtmlProjectedDocumentContentProvider } from './HtmlProjectedDocumentContentProvider';

export class HtmlPreviewDocumentContentProvider implements vscode.TextDocumentContentProvider {
    public static readonly scheme = `${HtmlProjectedDocument.scheme}-preview`;
    public static readonly previewUri
        = vscode.Uri.parse(`${HtmlPreviewDocumentContentProvider.scheme}://razor/Htmlpreview`);

    private onDidChangeEmitter = new vscode.EventEmitter<vscode.Uri>();

    constructor(private htmlProjectionProvider: HtmlProjectedDocumentContentProvider ) {
    }

    public get onDidChange() { return this.onDidChangeEmitter.event; }

    public update() {
        const projectedDocument = this.htmlProjectionProvider.getActiveDocument();

        if (projectedDocument) {
            this.onDidChangeEmitter.fire(HtmlPreviewDocumentContentProvider.previewUri);
        }
    }

    public async provideTextDocumentContent() {
        const projectedDocument = await this.htmlProjectionProvider.getActiveDocument();
        const projectedUriPath = projectedDocument.projectedUri.path;
        const document = vscode.workspace.textDocuments.find(
            doc => doc.uri.path.localeCompare(projectedUriPath, undefined, { sensitivity: 'base' }) === 0);

        if (document) {
            const content = document.getText();
            const htmlContent = `
                <body>
                    <p>For host document: <strong>${projectedDocument.hostDocumentUri.path}</strong></p>
                    <hr />
                    <pre>
                    ${content}
                    </pre>
                    <hr />
                </body>`;

            return htmlContent;
        } else {
            return '<body>No content...</body>';
        }
    }

    public async showRazorHtmlWindow() {
        const activeProjectedDocument = this.htmlProjectionProvider.getActiveDocument();

        if (!activeProjectedDocument) {
            vscode.window.showErrorMessage('No active text editor.');
            return;
        }

        try {
            await vscode.commands.executeCommand(
                'vscode.previewHtml',
                HtmlPreviewDocumentContentProvider.previewUri,
                vscode.ViewColumn.Two,
                'Razor Html Output');
        } catch (error) {
            vscode.window.showErrorMessage(error);
        }
    }
}
