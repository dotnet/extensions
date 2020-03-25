/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { RazorLanguageFeatureBase } from '../RazorLanguageFeatureBase';
import { LanguageKind } from '../RPC/LanguageKind';

export class RazorDocumentSemanticTokensProvider
    extends RazorLanguageFeatureBase
    implements vscode.DocumentSemanticTokensProvider {

    public async provideDocumentSemanticTokens(document: vscode.TextDocument, token: vscode.CancellationToken) {
        const semanticTokenResponse = await this.serviceClient.mapSemanticTokens(LanguageKind.Razor, document.uri);

        if (semanticTokenResponse) {
            // However we're serializing into Uint32Array doesn't set byteLength, which is checked by some stuff under the covers.
            // Solution? Create a new one, blat it over the old one, go home for the weekend.
            semanticTokenResponse.data = new Uint32Array(semanticTokenResponse.data);
        }

        return semanticTokenResponse;
    }
}
