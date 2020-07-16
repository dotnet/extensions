/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { RazorLanguageFeatureBase } from '../RazorLanguageFeatureBase';

export class RazorDocumentSemanticTokensProvider
    extends RazorLanguageFeatureBase
    implements vscode.DocumentSemanticTokensProvider, vscode.DocumentRangeSemanticTokensProvider {
    public async provideDocumentSemanticTokensEdits(
        document: vscode.TextDocument,
        previousResultId: string,
        token: vscode.CancellationToken,
    ): Promise<vscode.SemanticTokens | vscode.SemanticTokensEdits | undefined> {
        let semanticTokenResponse = await this.serviceClient.semanticTokensEdit(document.uri, previousResultId);

        if (semanticTokenResponse instanceof vscode.SemanticTokens) {
            // However we're serializing into Uint32Array doesn't set byteLength, which is checked by some stuff under the covers.
            // Solution? Create a new one, blat it over the old one, go home for the weekend.
            const fixedArray = new Uint32Array(semanticTokenResponse.data);
            semanticTokenResponse = new vscode.SemanticTokens(fixedArray, semanticTokenResponse.resultId);
        }

        return semanticTokenResponse;
    }

    public async provideDocumentRangeSemanticTokens(
        document: vscode.TextDocument,
        range: vscode.Range,
        token: vscode.CancellationToken,
    ): Promise<vscode.SemanticTokens | undefined> {
        let semanticRangeResponse = await this.serviceClient.semanticTokensRange(document.uri, range);

        if (semanticRangeResponse) {
            // However we're serializing into Uint32Array doesn't set byteLength, which is checked by some stuff under the covers.
            // Solution? Create a new one, blat it over the old one, go home for the weekend.
            const fixedArray = new Uint32Array(semanticRangeResponse.data);
            semanticRangeResponse = new vscode.SemanticTokens(fixedArray, semanticRangeResponse.resultId);
        }

        return semanticRangeResponse;
    }

    public async provideDocumentSemanticTokens(document: vscode.TextDocument, token: vscode.CancellationToken): Promise<vscode.SemanticTokens | undefined> {
        let semanticTokenResponse = await this.serviceClient.semanticTokens(document.uri);

        if (semanticTokenResponse) {
            // However we're serializing into Uint32Array doesn't set byteLength, which is checked by some stuff under the covers.
            // Solution? Create a new one, blat it over the old one, go home for the weekend.
            const fixedArray = new Uint32Array(semanticTokenResponse.data);
            semanticTokenResponse = new vscode.SemanticTokens(fixedArray, semanticTokenResponse.resultId);
        }

        return semanticTokenResponse;
    }
}
