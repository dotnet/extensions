/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { RazorLanguageServerClient } from './RazorLanguageServerClient';
import { AddProjectRequest } from './RPC/AddProjectRequest';
import { AddDocumentRequest } from './RPC/AddDocumentRequest';

export class RazorLanguageServiceClient {
    private _serverClient: RazorLanguageServerClient;

    constructor(serverClient: RazorLanguageServerClient) {
        this._serverClient = serverClient;
    }

    public async addProject(projectFileUri: vscode.Uri, configurationName?: string): Promise<void> {
        let request = new AddProjectRequest(projectFileUri.fsPath, configurationName);
        await this._serverClient.sendRequest<AddProjectRequest>("projects/addProject", request);
    }

    public async addDocument(document: vscode.TextDocument): Promise<void> {
        let request = new AddDocumentRequest(document);
        await this._serverClient.sendRequest<AddDocumentRequest>("projects/addDocument", request);
    }
}