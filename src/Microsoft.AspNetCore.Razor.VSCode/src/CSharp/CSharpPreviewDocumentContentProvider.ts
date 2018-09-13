/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { IRazorDocumentChangeEvent } from '../IRazorDocumentChangeEvent';
import { RazorDocumentChangeKind } from '../RazorDocumentChangeKind';
import { RazorDocumentManager } from '../RazorDocumentManager';
import { CSharpProjectedDocumentContentProvider } from './CSharpProjectedDocumentContentProvider';

export class CSharpPreviewDocumentContentProvider implements vscode.TextDocumentContentProvider {
    public static readonly scheme = `${CSharpProjectedDocumentContentProvider.scheme}-preview`;
    public static readonly previewUri
        = vscode.Uri.parse(`${CSharpPreviewDocumentContentProvider.scheme}://razor/csharppreview`);

    private readonly onDidChangeEmitter = new vscode.EventEmitter<vscode.Uri>();

    constructor(private readonly documentManager: RazorDocumentManager) {
        documentManager.onChange((event) => this.documentChanged(event));
    }

    public get onDidChange() { return this.onDidChangeEmitter.event; }

    public async provideTextDocumentContent() {
        const document = await this.documentManager.getActiveDocument();

        if (!document) {
            return '';
        }

        const content = document.csharpDocument.getContent();

        return `
            <body>
                <p>For host document: <strong>${document.path}</strong></p>
                <hr />
                <pre>${content}</pre>
                <hr />
            </body>`;
    }

    public async showRazorCSharpWindow() {
        const document = await this.documentManager.getActiveDocument();

        if (!document) {
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

    private async documentChanged(event: IRazorDocumentChangeEvent) {
        if (event.kind === RazorDocumentChangeKind.csharpChanged) {
            const document = await this.documentManager.getActiveDocument();

            if (document === event.document) {
                this.onDidChangeEmitter.fire(CSharpPreviewDocumentContentProvider.previewUri);
            }
        }
    }
}
