/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { RazorLanguage } from '../RazorLanguage';
import { HtmlProjectedDocument } from './HtmlProjectedDocument';

export class HtmlProjectedDocumentContentProvider implements vscode.TextDocumentContentProvider {
    public static readonly scheme = 'razor-html';

    private onDidChangeEmitter = new vscode.EventEmitter<vscode.Uri>();
    private projectedDocuments: { [hostDocumentPath: string]: HtmlProjectedDocument } = {};

    public get onDidChange() { return this.onDidChangeEmitter.event; }

    public async update(uri: vscode.Uri) {
        const projectedDocument = await this.ensureProjectedDocument(uri);

        const hostDocumentUriPath = projectedDocument.hostDocumentUri.path;
        const hostDocument = vscode.workspace.textDocuments.find(
            doc => doc.uri.path.localeCompare(hostDocumentUriPath, undefined, { sensitivity: 'base' }) === 0);

        if (hostDocument) {
            const hostDocumentText = hostDocument.getText();
            projectedDocument.setContent(hostDocumentText, hostDocument.version);
        }

        this.onDidChangeEmitter.fire(projectedDocument.projectedUri);
    }

    public provideTextDocumentContent(uri: vscode.Uri) {
        const projectedDocument = this.findProjectedDocument(uri);
        if (!projectedDocument) {
            vscode.window.showErrorMessage('For some reason the projected document isn\'t set.');
            return;
        }

        const content = projectedDocument.getContent();
        return content;
    }

    public getDocument(hostDocumentUri: vscode.Uri) {
        return this.ensureProjectedDocument(hostDocumentUri);
    }

    public getActiveDocument() {
        if (!vscode.window.activeTextEditor) {
            return null;
        }

        return this.ensureProjectedDocument(vscode.window.activeTextEditor.document.uri);
    }

    private async ensureProjectedDocument(hostDocumentUri: vscode.Uri) {
        let projectedDocument = this.projectedDocuments[hostDocumentUri.path];

        if (!projectedDocument) {
            projectedDocument = this.createProjectedDocument(hostDocumentUri);
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

    private createProjectedDocument(hostDocumentUri: vscode.Uri) {
        const extensionlessPath = hostDocumentUri.path.substring(
            0, hostDocumentUri.path.length - RazorLanguage.fileExtension.length - 1);
        const transformedPath =  `${extensionlessPath}.html`;
        const projectedUri = vscode.Uri.parse(`${HtmlProjectedDocumentContentProvider.scheme}://${transformedPath}`);
        const onChange = () => this.onDidChangeEmitter.fire(projectedUri);

        return new HtmlProjectedDocument(projectedUri, hostDocumentUri, onChange);
    }
}
