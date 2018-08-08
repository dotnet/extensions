/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { CSharpProjectedDocument } from './CSharpProjectedDocument';

export class CSharpProjectedDocumentContentProvider implements vscode.TextDocumentContentProvider {
    private onDidChangeEmitter = new vscode.EventEmitter<vscode.Uri>();
    private projectedDocuments: { [hostDocumentPath: string]: CSharpProjectedDocument } = {};

    public get onDidChange() { return this.onDidChangeEmitter.event; }

    public update(uri: vscode.Uri) {
        this.ensureProjectedDocument(uri)
            .then(projectedDocument => {
                this.onDidChangeEmitter.fire(projectedDocument.projectedUri);
            })
            .catch(reason => {
                vscode.window.showErrorMessage(`For some reason we failed to open the projected document: ${reason}`);
            });
    }

    public provideTextDocumentContent(uri: vscode.Uri) {
        const projectedDocument = this.findProjectedDocument(uri);
        if (!projectedDocument) {
            vscode.window.showErrorMessage('For some reason the projected document isn\'t set.');
            return;
        }

        const hostDocumentUriPath = projectedDocument.hostDocumentUri.path;
        const hostDocument = vscode.workspace.textDocuments.find(
            doc => doc.uri.path.localeCompare(hostDocumentUriPath, undefined, { sensitivity: 'base' }) === 0);

        let content = `// ${uri}\r\n`;

        if (hostDocument) {
            content += `\r\n${hostDocument.getText()}\r\n`;
        }

        return content;
    }

    public getDocument(hostDocumentUri: vscode.Uri) {
        return this.ensureProjectedDocument(hostDocumentUri);
    }

    public getActiveDocument() {
        if (!vscode.window.activeTextEditor) {
            throw new Error('No active text document');
        }

        return this.ensureProjectedDocument(vscode.window.activeTextEditor.document.uri);
    }

    private async ensureProjectedDocument(hostDocumentUri: vscode.Uri) {
        let projectedDocument = this.projectedDocuments[hostDocumentUri.path];

        if (!projectedDocument) {
            projectedDocument = CSharpProjectedDocument.create(hostDocumentUri);
            this.projectedDocuments[hostDocumentUri.path] = projectedDocument;
        }

        await vscode.workspace.openTextDocument(projectedDocument.projectedUri);
        return projectedDocument;
    }

    private findProjectedDocument(uri: vscode.Uri) {
        return Object.values(this.projectedDocuments)
            .find(document => document.projectedUri.path.localeCompare(
                uri.path, undefined, { sensitivity: 'base' }) === 0);
    }
}
