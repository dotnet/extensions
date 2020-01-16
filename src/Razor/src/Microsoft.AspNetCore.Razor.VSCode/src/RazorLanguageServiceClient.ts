/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { RazorLanguageServerClient } from './RazorLanguageServerClient';
import { RazorLogger } from './RazorLogger';
import { AddDocumentRequest } from './RPC/AddDocumentRequest';
import { LanguageKind } from './RPC/LanguageKind';
import { LanguageQueryRequest } from './RPC/LanguageQueryRequest';
import { LanguageQueryResponse } from './RPC/LanguageQueryResponse';
import { RazorMapToDocumentRangeRequest } from './RPC/RazorMapToDocumentRangeRequest';
import { RazorMapToDocumentRangeResponse } from './RPC/RazorMapToDocumentRangeResponse';
import { RemoveDocumentRequest } from './RPC/RemoveDocumentRequest';
import { convertRangeFromSerializable, convertRangeToSerializable } from './RPC/SerializableRange';

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

    public async languageQuery(position: vscode.Position, uri: vscode.Uri) {
        await this.ensureStarted();

        const request = new LanguageQueryRequest(position, uri);
        const response = await this.serverClient.sendRequest<LanguageQueryResponse>('razor/languageQuery', request);
        response.position = new vscode.Position(response.position.line, response.position.character);
        return response;
    }

    public async mapToDocumentRange(languageKind: LanguageKind, range: vscode.Range, uri: vscode.Uri) {
        await this.ensureStarted();

        const serializableRange = convertRangeToSerializable(range);
        const request = new RazorMapToDocumentRangeRequest(languageKind, serializableRange, uri);
        const response = await this.serverClient.sendRequest<RazorMapToDocumentRangeResponse>('razor/mapToDocumentRange', request);
        if (response.range.start.line >= 0) {
            const remappedRange = convertRangeFromSerializable(response.range);
            response.range = remappedRange;
            return response;
        }
    }

    private async ensureStarted() {
        // If the server is already started this will instantly return.
        await this.serverClient.start();
    }
}
