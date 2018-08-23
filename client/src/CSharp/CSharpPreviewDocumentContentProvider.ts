/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { CSharpProjectedDocumentContentProvider } from './CSharpProjectedDocumentContentProvider';

export class CSharpPreviewDocumentContentProvider implements vscode.TextDocumentContentProvider {
    public static readonly scheme = `${CSharpProjectedDocumentContentProvider.scheme}-preview`;
    public static readonly previewUri
        = vscode.Uri.parse(`${CSharpPreviewDocumentContentProvider.scheme}://razor/csharppreview`);

    private readonly onDidChangeEmitter = new vscode.EventEmitter<vscode.Uri>();

    constructor(private readonly csharpProjectionProvider: CSharpProjectedDocumentContentProvider ) {
        csharpProjectionProvider.onDidChange(uri => this.tryUpdate(uri));
    }

    public get onDidChange() { return this.onDidChangeEmitter.event; }

    public async provideTextDocumentContent() {
        const projectedDocument = await this.csharpProjectionProvider.getActiveDocument();

        if (!projectedDocument) {
            vscode.window.showErrorMessage('For some reason the projected document isn\'t set.');
            return '';
        }

        const content = projectedDocument.getContent();

        return `
            <body>
                <p>For host document: <strong>${projectedDocument.hostDocumentUri.path}</strong></p>
                <hr />
                <pre>${content}</pre>
                <hr />
            </body>`;
    }

    public async showRazorCSharpWindow() {
        const activeProjectedDocument = await this.csharpProjectionProvider.getActiveDocument();

        if (!activeProjectedDocument) {
            vscode.window.showErrorMessage('No active text editor.');
            return;
        }

        try {
            await vscode.commands.executeCommand(
                'vscode.previewHtml',
                CSharpPreviewDocumentContentProvider.previewUri,
                vscode.ViewColumn.Two,
                'Razor CSharp Output');
        } catch (error) {
            vscode.window.showErrorMessage(error);
        }
    }

    private async tryUpdate(uri: vscode.Uri) {
        const activeDocument = await this.csharpProjectionProvider.getActiveDocument();

        if (activeDocument && activeDocument.projectedUri === uri) {
            this.onDidChangeEmitter.fire(CSharpPreviewDocumentContentProvider.previewUri);
        }
    }
}
