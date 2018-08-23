/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { HtmlProjectedDocumentContentProvider } from './HtmlProjectedDocumentContentProvider';

export class HtmlPreviewDocumentContentProvider implements vscode.TextDocumentContentProvider {
    public static readonly scheme = `${HtmlProjectedDocumentContentProvider.scheme}-preview`;
    public static readonly previewUri
        = vscode.Uri.parse(`${HtmlPreviewDocumentContentProvider.scheme}://razor/htmlpreview`);

    private readonly onDidChangeEmitter = new vscode.EventEmitter<vscode.Uri>();

    constructor(private readonly htmlProjectionProvider: HtmlProjectedDocumentContentProvider ) {
        htmlProjectionProvider.onDidChange(uri => this.tryUpdate(uri));
    }

    public get onDidChange() { return this.onDidChangeEmitter.event; }

    public async provideTextDocumentContent() {
        const projectedDocument = await this.htmlProjectionProvider.getActiveDocument();

        if (!projectedDocument) {
            return '';
        }

        const content = projectedDocument.getContent();

        return `
            <body>
                <p>For host document: <strong>${projectedDocument.hostDocumentUri.path}</strong></p>
                <hr />
                <pre>${content.replace(/\</g, '&lt;')}</pre>
                <hr />
            </body>`;
    }

    public async showRazorHtmlWindow() {
        const activeProjectedDocument = await this.htmlProjectionProvider.getActiveDocument();

        if (!activeProjectedDocument) {
            vscode.window.showErrorMessage('No active text editor.');
            return;
        }

        try {
            await vscode.commands.executeCommand(
                'vscode.previewHtml',
                HtmlPreviewDocumentContentProvider.previewUri,
                vscode.ViewColumn.Two,
                'Razor HTML Output');
        } catch (error) {
            vscode.window.showErrorMessage(error);
        }
    }

    private async tryUpdate(uri: vscode.Uri) {
        const activeDocument = await this.htmlProjectionProvider.getActiveDocument();

        if (activeDocument && activeDocument.projectedUri === uri) {
            this.onDidChangeEmitter.fire(HtmlPreviewDocumentContentProvider.previewUri);
        }
    }
}
