/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { RazorLanguageServerClient } from './RazorLanguageServerClient';
import { AddProjectRequest } from './RPC/AddProjectRequest';
import { RemoveProjectRequest } from './RPC/RemoveProjectRequest';
import { LanguageQueryRequest } from './RPC/LanguageQueryRequest';
import { LanguageQueryResponse } from './RPC/LanguageQueryResponse';

export class RazorLanguageServiceClient {
    private _serverClient: RazorLanguageServerClient;

    constructor(serverClient: RazorLanguageServerClient) {
        this._serverClient = serverClient;
    }

    public async addProject(projectFileUri: vscode.Uri, configurationName?: string): Promise<void> {
        let request = new AddProjectRequest(projectFileUri.fsPath, configurationName);

        await this._serverClient.sendRequest<AddProjectRequest>("projects/addProject", request);
    }

    public async removeProject(projectFileUri: vscode.Uri): Promise<void> {
        let request = new RemoveProjectRequest(projectFileUri.fsPath);

        await this._serverClient.sendRequest<RemoveProjectRequest>("projects/removeProject", request);
    }

    public async languageQuery(position: vscode.Position, uri: vscode.Uri): Promise<LanguageQueryResponse> {
        let request = new LanguageQueryRequest(position, uri);

        let response = await this._serverClient.sendRequest<LanguageQueryResponse>("razor/languageQuery", request);
        response.position = new vscode.Position(response.position.line, response.position.character);

        return response;
    }
}