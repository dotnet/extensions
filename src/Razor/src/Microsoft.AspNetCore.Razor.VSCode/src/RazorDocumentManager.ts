/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { CSharpProjectedDocument } from './CSharp/CSharpProjectedDocument';
import { HtmlProjectedDocument } from './Html/HtmlProjectedDocument';
import { IRazorDocument } from './IRazorDocument';
import { IRazorDocumentChangeEvent } from './IRazorDocumentChangeEvent';
import { IRazorDocumentManager } from './IRazorDocumentManager';
import { RazorDocumentChangeKind } from './RazorDocumentChangeKind';
import { createDocument } from './RazorDocumentFactory';
import { RazorLanguage } from './RazorLanguage';
import { RazorLanguageServerClient } from './RazorLanguageServerClient';
import { RazorLogger } from './RazorLogger';
import { UpdateCSharpBufferRequest } from './RPC/UpdateCSharpBufferRequest';
import { getUriPath } from './UriPaths';

export class RazorDocumentManager implements IRazorDocumentManager {
    private readonly razorDocuments: { [hostDocumentPath: string]: IRazorDocument } = {};
    private onChangeEmitter = new vscode.EventEmitter<IRazorDocumentChangeEvent>();

    constructor(
        private readonly serverClient: RazorLanguageServerClient,
        private readonly logger: RazorLogger) {
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

        if (vscode.window.activeTextEditor.document.languageId !== RazorLanguage.id) {
            return null;
        }

        const activeDocument = await this.getDocument(vscode.window.activeTextEditor.document.uri);
        return activeDocument;
    }

    public async initialize() {
        // Track current documents
        const documentUris = await vscode.workspace.findFiles(RazorLanguage.globbingPattern);

        for (const uri of documentUris) {
            this.addDocument(uri);
        }

        const activeRazorDocument = await this.getActiveDocument();
        if (activeRazorDocument) {
            // Initialize the html buffer for the current document
            this.updateHtmlBuffer(activeRazorDocument);
        }

        for (const textDocument of vscode.workspace.textDocuments) {
            if (textDocument.languageId !== RazorLanguage.id) {
                continue;
            }

            if (textDocument.isClosed) {
                continue;
            }

            this.openDocument(textDocument.uri);
        }
    }

    public register() {
        // Track future documents
        const watcher = vscode.workspace.createFileSystemWatcher(RazorLanguage.globbingPattern);
        const didCreateRegistration = watcher.onDidCreate(
            async (uri: vscode.Uri) => this.addDocument(uri));
        const didDeleteRegistration = watcher.onDidDelete(
            async (uri: vscode.Uri) => this.removeDocument(uri));
        const didOpenRegistration = vscode.workspace.onDidOpenTextDocument(document => {
            if (document.languageId !== RazorLanguage.id) {
                return;
            }

            this.openDocument(document.uri);
        });
        const didCloseRegistration = vscode.workspace.onDidCloseTextDocument(document => {
            if (document.languageId !== RazorLanguage.id) {
                return;
            }

            this.closeDocument(document.uri);
        });
        const didChangeRegistration = vscode.workspace.onDidChangeTextDocument(async args => {
            if (args.document.languageId !== RazorLanguage.id) {
                return;
            }

            this.documentChanged(args.document.uri);
        });
        this.serverClient.onRequest(
            'updateCSharpBuffer',
            updateBufferRequest => this.updateCSharpBuffer(updateBufferRequest));

        return vscode.Disposable.from(
            watcher,
            didCreateRegistration,
            didDeleteRegistration,
            didOpenRegistration,
            didCloseRegistration,
            didChangeRegistration);
    }

    private _getDocument(uri: vscode.Uri) {
        const path = getUriPath(uri);
        const document = this.razorDocuments[path];

        if (!document) {
            throw new Error('Requested document does not exist.');
        }

        return document;
    }

    private openDocument(uri: vscode.Uri) {
        const document = this._getDocument(uri);

        this.updateHtmlBuffer(document);
        this.notifyDocumentChange(document, RazorDocumentChangeKind.opened);
    }

    private closeDocument(uri: vscode.Uri) {
        const document = this._getDocument(uri);

        const csharpDocument = document.csharpDocument;
        const csharpProjectedDocument = csharpDocument as CSharpProjectedDocument;
        const htmlDocument = document.htmlDocument;
        const htmlProjectedDocument = htmlDocument as HtmlProjectedDocument;

        // Reset the projected documents, VSCode resets all sync versions when a document closes.
        csharpProjectedDocument.reset();
        htmlProjectedDocument.reset();

        this.notifyDocumentChange(document, RazorDocumentChangeKind.closed);
    }

    private async documentChanged(uri: vscode.Uri) {
        const document = await this._getDocument(uri);

        const activeTextEditor = vscode.window.activeTextEditor;
        if (activeTextEditor && activeTextEditor.document.uri === uri) {
            this.updateHtmlBuffer(document);
        }
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
        if (this.logger.verboseEnabled) {
            this.logger.logVerbose(
                `Updating the C# document for Razor file '${updateBufferRequest.hostDocumentFilePath}' ` +
                `(${updateBufferRequest.hostDocumentVersion})`);
        }

        const hostDocumentUri = vscode.Uri.file(updateBufferRequest.hostDocumentFilePath);
        const document = this._getDocument(hostDocumentUri);
        const projectedDocument = document.csharpDocument;

        if (!projectedDocument.hostDocumentSyncVersion ||
            projectedDocument.hostDocumentSyncVersion <= updateBufferRequest.hostDocumentVersion) {
            // We allow re-setting of the updated content from the same doc sync version in the case
            // of project or file import changes.
            const csharpProjectedDocument = projectedDocument as CSharpProjectedDocument;
            csharpProjectedDocument.update(updateBufferRequest.changes, updateBufferRequest.hostDocumentVersion);

            this.notifyDocumentChange(document, RazorDocumentChangeKind.csharpChanged);
        }
    }

    private updateHtmlBuffer(document: IRazorDocument) {
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
        if (this.logger.verboseEnabled) {
            this.logger.logVerbose(
                `Notifying document '${getUriPath(document.uri)}' changed '${RazorDocumentChangeKind[kind]}'`);
        }

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
}
