/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { RazorLanguageServerClient } from './RazorLanguageServerClient';
import { AddProjectRequest } from './RPC/AddProjectRequest';
import { LanguageQueryRequest } from './RPC/LanguageQueryRequest';
import { LanguageQueryResponse } from './RPC/LanguageQueryResponse';
import { RemoveProjectRequest } from './RPC/RemoveProjectRequest';

export class RazorLanguageServiceClient {
    constructor(private readonly serverClient: RazorLanguageServerClient) {
    }

    public async addProject(projectFileUri: vscode.Uri, configurationName?: string) {
        const request = new AddProjectRequest(projectFileUri.fsPath, configurationName);
        await this.serverClient.sendRequest<AddProjectRequest>('projects/addProject', request);
    }

    public async removeProject(projectFileUri: vscode.Uri) {
        const request = new RemoveProjectRequest(projectFileUri.fsPath);
        await this.serverClient.sendRequest<RemoveProjectRequest>('projects/removeProject', request);
    }

    public async languageQuery(position: vscode.Position, uri: vscode.Uri) {
        const request = new LanguageQueryRequest(position, uri);
        const response = await this.serverClient.sendRequest<LanguageQueryResponse>('razor/languageQuery', request);
        response.position = new vscode.Position(response.position.line, response.position.character);
        return response;
    }
}
