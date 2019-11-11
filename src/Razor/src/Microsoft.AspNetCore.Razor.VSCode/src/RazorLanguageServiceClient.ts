/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { IRazorProject } from './IRazorProject';
import { RazorLanguage } from './RazorLanguage';
import { RazorLanguageServerClient } from './RazorLanguageServerClient';
import { RazorLogger } from './RazorLogger';
import { AddDocumentRequest } from './RPC/AddDocumentRequest';
import { AddProjectRequest } from './RPC/AddProjectRequest';
import { LanguageQueryRequest } from './RPC/LanguageQueryRequest';
import { LanguageQueryResponse } from './RPC/LanguageQueryResponse';
import { RazorTextDocumentItem } from './RPC/RazorTextDocumentItem';
import { RemoveDocumentRequest } from './RPC/RemoveDocumentRequest';
import { RemoveProjectRequest } from './RPC/RemoveProjectRequest';
import { UpdateProjectRequest } from './RPC/UpdateProjectRequest';

export class RazorLanguageServiceClient {
    constructor(
        private readonly serverClient: RazorLanguageServerClient,
        private readonly logger: RazorLogger) {
    }

    public async addDocument(documentUri: vscode.Uri) {
        await this.ensureStarted();

        const request = new AddDocumentRequest(documentUri.fsPath);
        await this.serverClient.sendRequest<AddDocumentRequest>('projects/addDocument', request);
    }

    public async removeDocument(documentUri: vscode.Uri) {
        await this.ensureStarted();

        const request = new RemoveDocumentRequest(documentUri.fsPath);
        await this.serverClient.sendRequest<RemoveDocumentRequest>('projects/removeDocument', request);
    }

    public async addProject(projectFileUri: vscode.Uri) {
        await this.ensureStarted();

        const request = new AddProjectRequest(projectFileUri.fsPath);
        await this.serverClient.sendRequest<AddProjectRequest>('projects/addProject', request);
    }

    public async removeProject(projectFileUri: vscode.Uri) {
        await this.ensureStarted();

        const request = new RemoveProjectRequest(projectFileUri.fsPath);
        await this.serverClient.sendRequest<RemoveProjectRequest>('projects/removeProject', request);
    }

    public async updateProject(project: IRazorProject) {
        await this.ensureStarted();

        const request: UpdateProjectRequest = {
            ProjectSnapshotHandle: {
                FilePath: project.uri.fsPath,
                ProjectWorkspaceState: project.configuration ? project.configuration.projectWorkspaceState : null,
                Configuration: project.configuration ? project.configuration.configuration : undefined,
                RootNamespace: project.configuration ? project.configuration.rootNamespace : undefined,
                Documents: project.configuration ? project.configuration.documents : undefined,
                SerializationFormat: project.configuration ? project.configuration.serializationFormat : null,
            },
        };
        await this.serverClient.sendRequest<UpdateProjectRequest>('projects/updateProject', request);
    }

    public async languageQuery(position: vscode.Position, uri: vscode.Uri) {
        await this.ensureStarted();

        const request = new LanguageQueryRequest(position, uri);
        const response = await this.serverClient.sendRequest<LanguageQueryResponse>('razor/languageQuery', request);
        response.position = new vscode.Position(response.position.line, response.position.character);
        return response;
    }

    private async ensureStarted() {
        // If the server is already started this will instantly return.
        await this.serverClient.start();
    }
}
