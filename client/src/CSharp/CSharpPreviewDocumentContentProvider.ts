/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { CSharpProjectedDocument } from './CSharpProjectedDocument';
import { CSharpProjectedDocumentContentProvider } from './CSharpProjectedDocumentContentProvider';

export class CSharpPreviewDocumentContentProvider implements vscode.TextDocumentContentProvider {
    public static readonly scheme: string = CSharpProjectedDocument.scheme + "-preview";
    public static readonly previewUri: vscode.Uri = vscode.Uri.parse(CSharpPreviewDocumentContentProvider.scheme + "://razor/csharppreview");

    private _onDidChange: vscode.EventEmitter<vscode.Uri>;
    private _csharpProjectionProvider: CSharpProjectedDocumentContentProvider;

    constructor (csharpContentProvider: CSharpProjectedDocumentContentProvider ) {
        this._csharpProjectionProvider = csharpContentProvider;
        this._onDidChange = new vscode.EventEmitter<vscode.Uri>();
    }

    public get onDidChange(): vscode.Event<vscode.Uri> {
        return this._onDidChange.event;
    }

    public update() {
        let projectedDocument = this._csharpProjectionProvider.getActiveDocument();

        if (!projectedDocument) {
            return;
        }

        this._onDidChange.fire(CSharpPreviewDocumentContentProvider.previewUri);
    }
    
    public async provideTextDocumentContent(): Promise<string> {
        let projectedDocument = await this._csharpProjectionProvider.getActiveDocument();

        if (!projectedDocument) {
            vscode.window.showErrorMessage("For some reason the projected document isn't set.");
            return "";
        }

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
                    <pre>${content}</pre>
                    <hr />
                </body>`;

            return htmlContent;
        }
        else {
            return "";
        }
    }

    public async showRazorCSharpWindow(): Promise<void> {
        let activeProjectedDocument = this._csharpProjectionProvider.getActiveDocument();
    
        if (!activeProjectedDocument) {
            vscode.window.showErrorMessage("No active text editor.");
            return;
        }
    
        try {
            await vscode.commands.executeCommand('vscode.previewHtml', CSharpPreviewDocumentContentProvider.previewUri, vscode.ViewColumn.Two, 'Razor CSharp Output');
        }
        catch (error) {
            vscode.window.showErrorMessage(error);
        }
    }
}