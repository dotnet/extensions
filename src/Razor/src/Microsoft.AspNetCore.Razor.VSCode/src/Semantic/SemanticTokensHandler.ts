/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

import * as vscode from 'vscode';
import { RequestType } from 'vscode-languageclient';
import { RazorLanguageServerClient } from '../RazorLanguageServerClient';
import { SerializableSemanticTokensParams } from '../RPC/SerializableSemanticTokensParams';
import { ProvideSemanticTokensResponse } from './ProvideSemanticTokensResponse';
import { SemanticTokensResponse } from './SemanticTokensResponse';

export class SemanticTokensHandler {
    private static readonly getSemanticTokensEndpoint = 'razor/provideSemanticTokens';
    private semanticTokensRequestType: RequestType<SerializableSemanticTokensParams, ProvideSemanticTokensResponse, any, any> = new RequestType(SemanticTokensHandler.getSemanticTokensEndpoint);
    private emptySemanticTokensResponse: ProvideSemanticTokensResponse = new ProvideSemanticTokensResponse(
        new SemanticTokensResponse(new Array<number>(), ''),
        null);

    constructor(private readonly serverClient: RazorLanguageServerClient) {
    }

    public register() {
        // tslint:disable-next-line: no-floating-promises
        this.serverClient.onRequestWithParams<SerializableSemanticTokensParams, ProvideSemanticTokensResponse, any, any>(
            this.semanticTokensRequestType,
            async (request, token) => this.getSemanticTokens(request, token));
    }

    private async getSemanticTokens(
        semanticTokensParams: SerializableSemanticTokensParams,
        cancellationToken: vscode.CancellationToken): Promise<ProvideSemanticTokensResponse> {

        // This is currently a No-Op because we don't have a way to get the semantic tokens from CSharp.
        // Other functions accomplish this with `vscode.execute<Blank>Provider`, but that doesn't exiset for Semantic Tokens yet because it's still not an official part of the spec.
        return this.emptySemanticTokensResponse;
    }
}
