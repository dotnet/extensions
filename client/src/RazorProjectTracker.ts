/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';

import { RazorLanguageServiceClient } from './RazorLanguageServiceClient';

export class RazorProjectTracker {
    constructor(private readonly languageServiceClient: RazorLanguageServiceClient) {
    }

    public async initialize() {
        // Track current projects
        const projectUris = await vscode.workspace.findFiles('**/*.csproj');

        for (const uri of projectUris) {
            await this.languageServiceClient.addProject(uri);
        }
    }

    public register() {
        // Track future projects
        const watcher = vscode.workspace.createFileSystemWatcher('**/*.csproj*');
        const createRegistration = watcher.onDidCreate(async (uri: vscode.Uri) => {
            await this.languageServiceClient.addProject(uri);
        });

        const deleteRegistration = watcher.onDidDelete(async (uri: vscode.Uri) => {
            await this.languageServiceClient.removeProject(uri);
        });

        return vscode.Disposable.from(watcher, createRegistration, deleteRegistration);
    }
}
