/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { RazorLanguageServerClient } from './RazorLanguageServerClient';
import { LanguageKind } from './RPC/LanguageKind';
import { LanguageQueryRequest } from './RPC/LanguageQueryRequest';
import { LanguageQueryResponse } from './RPC/LanguageQueryResponse';
import { RazorMapToDocumentRangeRequest } from './RPC/RazorMapToDocumentRangeRequest';
import { RazorMapToDocumentRangeResponse } from './RPC/RazorMapToDocumentRangeResponse';
import { convertRangeFromSerializable, convertRangeToSerializable } from './RPC/SerializableRange';
import { SemanticTokensRequest } from './Semantic/SemanticTokensRequest';

export class RazorLanguageServiceClient {
    constructor(private readonly serverClient: RazorLanguageServerClient) {
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

    public async getSemanticTokenLegend(): Promise<vscode.SemanticTokensLegend | undefined> {
        await this.ensureStarted();

        const response = await this.serverClient.sendRequest<vscode.SemanticTokensLegend>('_ms_/textDocument/semanticTokensLegend', /*request param*/null);

        if (response.tokenTypes && response.tokenTypes.length > 0) {
            return response;
        }
    }

    public async mapSemanticTokens(languageKind: LanguageKind, uri: vscode.Uri): Promise<vscode.SemanticTokens | undefined> {
        await this.ensureStarted();

        const request = new SemanticTokensRequest(languageKind, uri);
        const response = await this.serverClient.sendRequest<vscode.SemanticTokens>('_ms_/textDocument/semanticTokens', request);

        if (response.data && response.data.length > 0) {
            return response;
        }
    }

    private async ensureStarted() {
        // If the server is already started this will instantly return.
        await this.serverClient.start();
    }
}
