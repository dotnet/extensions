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
import { UpdateBufferRequest } from './RPC/UpdateBufferRequest';
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
        this.serverClient.onRequest(
            'razor/updateCSharpBuffer',
            async updateBufferRequest => this.updateCSharpBuffer(updateBufferRequest));

        this.serverClient.onRequest(
            'razor/updateHtmlBuffer',
            async updateBufferRequest => this.updateHtmlBuffer(updateBufferRequest));

        return vscode.Disposable.from(
            watcher,
            didCreateRegistration,
            didDeleteRegistration,
            didOpenRegistration,
            didCloseRegistration);
    }

    private _getDocument(uri: vscode.Uri) {
        const path = getUriPath(uri);
        let document = this.razorDocuments[path];

        // This might happen in the case that a file is opened outside the workspace
        if (!document) {
            this.logger.logMessage(`File '${path}' didn't exist in the Razor document list. This is likely because it's from outside the workspace.`);
            document = this.addDocument(uri);
        }

        return document;
    }

    private openDocument(uri: vscode.Uri) {
        const document = this._getDocument(uri);

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

        // Files outside of the workspace will return undefined from getWorkspaceFolder
        const workspaceFolder = vscode.workspace.getWorkspaceFolder(uri);
        if (!workspaceFolder) {
            // Out of workspace files should be removed once they're closed
            this.removeDocument(uri);
        }

        this.notifyDocumentChange(document, RazorDocumentChangeKind.closed);
    }

    private addDocument(uri: vscode.Uri) {
        const path = getUriPath(uri);
        let document = this.razorDocuments[path];
        if (document) {
            this.logger.logMessage(`Skipping document creation for '${path}' because it already exists.`);
            return document;
        }

        document = createDocument(uri);
        this.razorDocuments[document.path] = document;

        this.notifyDocumentChange(document, RazorDocumentChangeKind.added);

        return document;
    }

    private removeDocument(uri: vscode.Uri) {
        const document = this._getDocument(uri);
        delete this.razorDocuments[document.path];

        this.notifyDocumentChange(document, RazorDocumentChangeKind.removed);
    }

    private async updateCSharpBuffer(updateBufferRequest: UpdateBufferRequest) {
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
        } else {
            this.logger.logWarning('Failed to update the C# document buffer. This is unexpected and may result in incorrect C# interactions.');
        }
    }

    private async updateHtmlBuffer(updateBufferRequest: UpdateBufferRequest) {
        if (this.logger.verboseEnabled) {
            this.logger.logVerbose(
                `Updating the HTML document for Razor file '${updateBufferRequest.hostDocumentFilePath}' ` +
                `(${updateBufferRequest.hostDocumentVersion})`);
        }

        const hostDocumentUri = vscode.Uri.file(updateBufferRequest.hostDocumentFilePath);
        const document = this._getDocument(hostDocumentUri);
        const projectedDocument = document.htmlDocument;

        if (!projectedDocument.hostDocumentSyncVersion ||
            projectedDocument.hostDocumentSyncVersion <= updateBufferRequest.hostDocumentVersion) {
            // We allow re-setting of the updated content from the same doc sync version in the case
            // of project or file import changes.
            const htmlProjectedDocument = projectedDocument as HtmlProjectedDocument;
            htmlProjectedDocument.update(updateBufferRequest.changes, updateBufferRequest.hostDocumentVersion);

            this.notifyDocumentChange(document, RazorDocumentChangeKind.htmlChanged);
        } else {
            this.logger.logWarning('Failed to update the HTML document buffer. This is unexpected and may result in incorrect HTML interactions.');
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
