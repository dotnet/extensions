/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { CSharpProjectedDocument } from './CSharp/CSharpProjectedDocument';
import { HtmlProjectedDocument } from './Html/HtmlProjectedDocument';
import { IRazorDocument } from './IRazorDocument';
import { IRazorDocumentChangeEvent } from './IRazorDocumentChangeEvent';
import { RazorDocumentChangeKind } from './RazorDocumentChangeKind';
import { createDocument } from './RazorDocumentFactory';
import { RazorLanguage } from './RazorLanguage';
import { RazorLanguageServerClient } from './RazorLanguageServerClient';
import { UpdateCSharpBufferRequest } from './RPC/UpdateCSharpBufferRequest';
import { getUriPath } from './UriPaths';

const globbingPath = `**/*.${RazorLanguage.fileExtension}`;

export class RazorDocumentManager {
    private readonly razorDocuments: { [hostDocumentPath: string]: IRazorDocument } = {};
    private onChangeEmitter = new vscode.EventEmitter<IRazorDocumentChangeEvent>();

    constructor(
        private readonly serverClient: RazorLanguageServerClient) {
    }

    public get onChange() { return this.onChangeEmitter.event; }

    public get documents() {
        return Object.values(this.razorDocuments);
    }

    public async getDocument(uri: vscode.Uri) {
        const document = this._getDocument(uri);

        await this.ensureProjectedDocumentsOpen(document);

        return document;
    }

    public async getActiveDocument() {
        if (!vscode.window.activeTextEditor) {
            return null;
        }

        const activeDocument = await this.getDocument(vscode.window.activeTextEditor.document.uri);
        return activeDocument;
    }

    public async initialize() {
        // Track current documents
        const documentUris = await vscode.workspace.findFiles(globbingPath);

        for (const uri of documentUris) {
            this.addDocument(uri);
        }

        const activeRazorDocument = await this.getActiveDocument();
        if (activeRazorDocument) {
            this.updateHtmlBuffer(activeRazorDocument.uri);
        }
    }

    public register() {
        // Track future documents
        const watcher = vscode.workspace.createFileSystemWatcher(globbingPath);
        const createRegistration = watcher.onDidCreate(
            async (uri: vscode.Uri) => this.addDocument(uri));
        const deleteRegistration = watcher.onDidDelete(
            async (uri: vscode.Uri) => this.removeDocument(uri));
        const didChangeRegistration = vscode.workspace.onDidChangeTextDocument(async args => {
            if (args.document.languageId !== RazorLanguage.id) {
                return;
            }

            const activeTextEditor = vscode.window.activeTextEditor;
            if (activeTextEditor && activeTextEditor.document === args.document) {
                this.updateHtmlBuffer(args.document.uri);
            }
        });
        this.serverClient.onRequest(
            'updateCSharpBuffer',
            updateBufferRequest => this.updateCSharpBuffer(updateBufferRequest));

        return vscode.Disposable.from(
            watcher,
            createRegistration,
            deleteRegistration,
            didChangeRegistration);
    }

    private _getDocument(uri: vscode.Uri) {
        const path = this.getUriPath(uri);
        const document = this.razorDocuments[path];

        if (!document) {
            throw new Error('Requested document does not exist.');
        }

        return document;
    }

    private addDocument(uri: vscode.Uri) {
        const document = createDocument(uri);
        this.razorDocuments[document.path] = document;

        this.notifyDocumentChange(document, RazorDocumentChangeKind.added);

        return document;
    }

    private removeDocument(uri: vscode.Uri) {
        const document = this._getDocument(uri);
        delete this.razorDocuments[document.path];

        this.notifyDocumentChange(document, RazorDocumentChangeKind.removed);
    }

    private async updateCSharpBuffer(updateBufferRequest: UpdateCSharpBufferRequest) {
        const hostDocumentUri = vscode.Uri.file(updateBufferRequest.hostDocumentFilePath);
        const document = this._getDocument(hostDocumentUri);
        const projectedDocument = document.csharpDocument;

        if (!projectedDocument.hostDocumentSyncVersion ||
            projectedDocument.hostDocumentSyncVersion < updateBufferRequest.hostDocumentVersion) {

            const csharpProjectedDocument = projectedDocument as CSharpProjectedDocument;
            csharpProjectedDocument.update(updateBufferRequest.changes, updateBufferRequest.hostDocumentVersion);

            this.notifyDocumentChange(document, RazorDocumentChangeKind.csharpChanged);
        }
    }

    private async updateHtmlBuffer(uri: vscode.Uri) {
        const document = await this._getDocument(uri);
        const projectedDocument = document.htmlDocument;

        const hostDocument = vscode.workspace.textDocuments.find(
            doc => getUriPath(doc.uri).localeCompare(document.path, undefined, { sensitivity: 'base' }) === 0);

        if (hostDocument) {
            const hostDocumentText = hostDocument.getText();
            const htmlProjectedDocument = projectedDocument as HtmlProjectedDocument;
            htmlProjectedDocument.setContent(hostDocumentText, hostDocument.version);

            this.notifyDocumentChange(document, RazorDocumentChangeKind.htmlChanged);
        }
    }

    private notifyDocumentChange(document: IRazorDocument, kind: RazorDocumentChangeKind) {
        const args: IRazorDocumentChangeEvent = {
            document,
            kind,
        };

        this.onChangeEmitter.fire(args);
    }

    private async ensureProjectedDocumentsOpen(document: IRazorDocument) {
        await vscode.workspace.openTextDocument(document.csharpDocument.uri);
        await vscode.workspace.openTextDocument(document.htmlDocument.uri);
    }

    private getUriPath(uri: vscode.Uri) {
        return uri.fsPath ? uri.fsPath : uri.path;
    }
}
