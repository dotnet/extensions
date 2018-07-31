/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { CSharpProjectedDocument } from './CSharpProjectedDocument';

export class CSharpProjectedDocumentContentProvider implements vscode.TextDocumentContentProvider {
    private _onDidChange: vscode.EventEmitter<vscode.Uri>;
    private _projectedDocuments: { [hostDocumentPath: string]: CSharpProjectedDocument };

    constructor () {
        this._onDidChange = new vscode.EventEmitter<vscode.Uri>();
        this._projectedDocuments = {};
    }

    public get onDidChange(): vscode.Event<vscode.Uri> {
        return this._onDidChange.event;
    }

    public update(uri: vscode.Uri) {
        this.ensureProjectedDocument(uri).then((projectedDocument) => {
            this._onDidChange.fire(projectedDocument.projectedUri);
        },
        (reason) => {
            vscode.window.showErrorMessage("For some reason we failed to open the projected document: " + reason);
        });
    }
    
    public provideTextDocumentContent(uri: vscode.Uri): vscode.ProviderResult<string> {
        let projectedDocument: CSharpProjectedDocument | undefined;
        for (let hostDocumentPath in this._projectedDocuments) {
            let document = this._projectedDocuments[hostDocumentPath];
            
            if (document.projectedUri.path.localeCompare(uri.path, undefined, { sensitivity: 'base' }) === 0) {
                projectedDocument = document;
            }
        }

        if (!projectedDocument) {
            vscode.window.showErrorMessage("For some reason the projected document isn't set.");
            return;
        }

        let hostDocumentUriPath = projectedDocument.hostDocumentUri.path;
        let hostDocument = vscode.workspace.textDocuments.find((doc) => {
            if (doc.uri.path.localeCompare(hostDocumentUriPath, undefined, { sensitivity: 'base' }) === 0) {
                return true;
            }

            return false;
        });

        var content = "// " + uri + "\r\n";

        if (hostDocument) {
            content += "\r\n" + hostDocument.getText() + "\r\n";
        }

        return content;
    }

    private async ensureProjectedDocument(hostDocumentUri: vscode.Uri): Promise<CSharpProjectedDocument> {
        let projectedDocument = this._projectedDocuments[hostDocumentUri.path];

        if (!projectedDocument) {
            projectedDocument = CSharpProjectedDocument.create(hostDocumentUri);
            this._projectedDocuments[hostDocumentUri.path] = projectedDocument;
        }

        await vscode.workspace.openTextDocument(projectedDocument.projectedUri);
        return projectedDocument;
    }

    public getDocument(hostDocumentUri: vscode.Uri): Promise<CSharpProjectedDocument> {
        return this.ensureProjectedDocument(hostDocumentUri);
    }

    public async getActiveDocument(): Promise<CSharpProjectedDocument> {
        if (!vscode.window.activeTextEditor) {
            throw new Error("No active text document");
        }

        let projectedDocument = await this.ensureProjectedDocument(vscode.window.activeTextEditor.document.uri);

        return projectedDocument;
    }
}