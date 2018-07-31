/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';

import { RazorLanguageServiceClient } from './RazorLanguageServiceClient';

export class RazorProjectTracker {
    private _languageServiceClient: RazorLanguageServiceClient;

    constructor(languageServiceClient: RazorLanguageServiceClient) {
        this._languageServiceClient = languageServiceClient;
    }

    public async initialize() {
        // Track current projects
        var projectUris = await vscode.workspace.findFiles("**/*.csproj");

        for (let i = 0; i < projectUris.length; i++) {
            await this._languageServiceClient.addProject(projectUris[i]);
        }
    }

    public register(): vscode.Disposable {
        // Track future projects
        let watcher = vscode.workspace.createFileSystemWatcher('**/*.csproj*');
        let createRegistration = watcher.onDidCreate(async (uri: vscode.Uri) => {
            await this._languageServiceClient.addProject(uri);
        });

        // TODO: Track delete

        return vscode.Disposable.from(watcher, createRegistration);
    }
}