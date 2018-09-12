/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { IRazorDocumentChangeEvent } from '../IRazorDocumentChangeEvent';
import { RazorDocumentChangeKind } from '../RazorDocumentChangeKind';
import { RazorDocumentManager } from '../RazorDocumentManager';
import { HtmlProjectedDocumentContentProvider } from './HtmlProjectedDocumentContentProvider';

export class HtmlPreviewDocumentContentProvider implements vscode.TextDocumentContentProvider {
    public static readonly scheme = `${HtmlProjectedDocumentContentProvider.scheme}-preview`;
    public static readonly previewUri
        = vscode.Uri.parse(`${HtmlPreviewDocumentContentProvider.scheme}://razor/htmlpreview`);

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

        const content = document.htmlDocument.getContent();

        return `
            <body>
                <p>For host document: <strong>${document.path}</strong></p>
                <hr />
                <pre>${content.replace(/\</g, '&lt;')}</pre>
                <hr />
            </body>`;
    }

    public async showRazorHtmlWindow() {
        const document = await this.documentManager.getActiveDocument();

        if (!document) {
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

    private async documentChanged(event: IRazorDocumentChangeEvent) {
        if (event.kind === RazorDocumentChangeKind.htmlChanged) {
            const document = await this.documentManager.getActiveDocument();

            if (document === event.document) {
                this.onDidChangeEmitter.fire(HtmlPreviewDocumentContentProvider.previewUri);
            }
        }
    }
}
