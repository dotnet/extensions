/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { HtmlProjectedDocument } from './HtmlProjectedDocument';
import { HtmlProjectedDocumentContentProvider } from './HtmlProjectedDocumentContentProvider';

export class HtmlPreviewDocumentContentProvider implements vscode.TextDocumentContentProvider {
    public static readonly scheme: string = HtmlProjectedDocument.scheme + "-preview";
    public static readonly previewUri: vscode.Uri = vscode.Uri.parse(HtmlPreviewDocumentContentProvider.scheme + "://razor/Htmlpreview");

    private _onDidChange: vscode.EventEmitter<vscode.Uri>;
    private _htmlProjectionProvider: HtmlProjectedDocumentContentProvider;

    constructor (HtmlContentProvider: HtmlProjectedDocumentContentProvider ) {
        this._htmlProjectionProvider = HtmlContentProvider;
        this._onDidChange = new vscode.EventEmitter<vscode.Uri>();
    }

    public get onDidChange(): vscode.Event<vscode.Uri> {
        return this._onDidChange.event;
    }

    public update() {
        let projectedDocument = this._htmlProjectionProvider.getActiveDocument();

        if (!projectedDocument) {
            return;
        }

        this._onDidChange.fire(HtmlPreviewDocumentContentProvider.previewUri);
    }

    public async provideTextDocumentContent(): Promise<string> {
        let projectedDocument = await this._htmlProjectionProvider.getActiveDocument();
        let projectedUriPath = projectedDocument.projectedUri.path;
        let document = vscode.workspace.textDocuments.find((doc) => {
            if (doc.uri.path.localeCompare(projectedUriPath, undefined, { sensitivity: 'base' }) === 0) {
                return true;
            }

            return false;
        });

        if (document) {
            let content = document.getText();
            let htmlContent = `
                <body>
                    <p>For host document: <strong>${projectedDocument.hostDocumentUri.path}</strong></p>
                    <hr />
                    <pre>
                    ${content}
                    </pre>
                    <hr />
                </body>`;

            return htmlContent;
        }
        else {
            return "<body>No content...</body>";
        }
    }
    
    public async showRazorHtmlWindow(): Promise<void> {
        let activeProjectedDocument = this._htmlProjectionProvider.getActiveDocument();
    
        if (!activeProjectedDocument) {
            vscode.window.showErrorMessage("No active text editor.");
            return;
        }
    
        try {
            await vscode.commands.executeCommand('vscode.previewHtml', HtmlPreviewDocumentContentProvider.previewUri, vscode.ViewColumn.Two, 'Razor Html Output');
        }
        catch (error) {
            vscode.window.showErrorMessage(error);
        }
    }
}