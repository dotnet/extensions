/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { CSharpProjectedDocument } from './CSharpProjectedDocument';
import { CSharpProjectedDocumentContentProvider } from './CSharpProjectedDocumentContentProvider';

export class CSharpPreviewDocumentContentProvider implements vscode.TextDocumentContentProvider {
    public static readonly scheme = `${CSharpProjectedDocument.scheme}-preview`;
    public static readonly previewUri
        = vscode.Uri.parse(`${CSharpPreviewDocumentContentProvider.scheme}://razor/csharppreview`);

    private readonly onDidChangeEmitter = new vscode.EventEmitter<vscode.Uri>();

    constructor(private readonly csharpProjectionProvider: CSharpProjectedDocumentContentProvider ) {
    }

    public get onDidChange() { return this.onDidChangeEmitter.event; }

    public update() {
        const projectedDocument = this.csharpProjectionProvider.getActiveDocument();

        if (projectedDocument) {
            this.onDidChangeEmitter.fire(CSharpPreviewDocumentContentProvider.previewUri);
        }
    }

    public async provideTextDocumentContent() {
        const projectedDocument = await this.csharpProjectionProvider.getActiveDocument();

        if (!projectedDocument) {
            vscode.window.showErrorMessage('For some reason the projected document isn\'t set.');
            return '';
        }

        const projectedUriPath = projectedDocument.projectedUri.path;
        const document = vscode.workspace.textDocuments.find(doc => {
            if (doc.uri.path.localeCompare(projectedUriPath, undefined, { sensitivity: 'base' }) === 0) {
                return true;
            }

            return false;
        });

        if (document) {
            const content = document.getText();
            return `
                <body>
                    <p>For host document: <strong>${projectedDocument.hostDocumentUri.path}</strong></p>
                    <hr />
                    <pre>${content}</pre>
                    <hr />
                </body>`;
        } else {
            return '';
        }
    }

    public async showRazorCSharpWindow() {
        const activeProjectedDocument = this.csharpProjectionProvider.getActiveDocument();

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
}
