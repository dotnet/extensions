/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';

import { RazorLanguage } from './RazorLanguage';
import { RazorLanguageServiceClient } from './RazorLanguageServiceClient';

const globbingPath = `**/*.${RazorLanguage.fileExtension}`;

export class RazorDocumentTracker {

    constructor(private readonly languageServiceClient: RazorLanguageServiceClient) {
    }

    public async initialize() {
        // Track current documents
        const documentUris = await vscode.workspace.findFiles(globbingPath);

        for (const uri of documentUris) {
            await this.languageServiceClient.addDocument(uri);
        }
    }

    public register() {
        // Track future documents
        const watcher = vscode.workspace.createFileSystemWatcher(globbingPath);
        const createRegistration = watcher.onDidCreate(async (uri: vscode.Uri) => {
            await this.languageServiceClient.addDocument(uri);
        });

        const deleteRegistration = watcher.onDidDelete(async (uri: vscode.Uri) => {
            await this.languageServiceClient.removeDocument(uri);
        });

        return vscode.Disposable.from(watcher, createRegistration, deleteRegistration);
    }
}
