/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { HtmlProjectedDocument } from './HtmlProjectedDocument';

export class HtmlProjectedDocumentContentProvider implements vscode.TextDocumentContentProvider {
    private onDidChangeEmitter = new vscode.EventEmitter<vscode.Uri>();
    private projectedDocuments: { [hostDocumentPath: string]: HtmlProjectedDocument } = {};

    public get onDidChange() { return this.onDidChangeEmitter.event; }

    public update(uri: vscode.Uri) {
        this.ensureProjectedDocument(uri)
            .then(projectedDocument => {
                this.onDidChangeEmitter.fire(projectedDocument.projectedUri);
            })
            .catch(reason => {
                vscode.window.showErrorMessage(
                    `For some reason we failed to open the projected HTML document: ${reason}`);
            });
    }

    public provideTextDocumentContent(uri: vscode.Uri) {
        const projectedDocument = this.findProjectedDocument(uri);
        if (!projectedDocument) {
            vscode.window.showErrorMessage('For some reason the projected html document isn\'t set.');
            return;
        }

        const hostDocumentUriPath = projectedDocument.hostDocumentUri.path;
        const hostDocument = vscode.workspace.textDocuments.find(
            doc => doc.uri.path.localeCompare(hostDocumentUriPath, undefined, { sensitivity: 'base' }) === 0);

        return hostDocument ? hostDocument.getText() : '';
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
            projectedDocument = HtmlProjectedDocument.create(hostDocumentUri);
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
