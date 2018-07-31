/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';

import { RazorLanguageServiceClient } from './RazorLanguageServiceClient';
import { RazorLanguage } from './RazorLanguage';

export class RazorDocumentTracker {
    private _languageServiceClient: RazorLanguageServiceClient;

    constructor(languageServiceClient: RazorLanguageServiceClient) {
        this._languageServiceClient = languageServiceClient;
    }

    public async initialize() {
        // Track currently open documents
        for (let i = 0; i < vscode.workspace.textDocuments.length; i++) {
            let document = vscode.workspace.textDocuments[i];

            if (this.isRazorDocument(document)) {
                await this._languageServiceClient.addDocument(document);
            }
        }
    }

    public register(): vscode.Disposable {
        // Track future documents
        let onOpenRegistration = vscode.workspace.onDidOpenTextDocument(async document => {
            if (this.isRazorDocument(document)) {
                await this._languageServiceClient.addDocument(document);
            }
        });

        // TODO: Implement close
        return vscode.Disposable.from(onOpenRegistration);
    }

    private isRazorDocument(document: vscode.TextDocument) {
        if (document.languageId === RazorLanguage.id) {
            return true;
        }

        return false;
    }
}