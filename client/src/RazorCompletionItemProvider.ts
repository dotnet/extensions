/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { RazorCSharpFeature } from './CSharp/RazorCSharpFeature';
import { RazorHtmlFeature } from './Html/RazorHtmlFeature';
import { RazorLanguageServiceClient } from './RazorLanguageServiceClient';
import { LanguageKind } from './RPC/LanguageKind';

export class RazorCompletionItemProvider implements vscode.CompletionItemProvider {
    constructor(private readonly csharpFeature: RazorCSharpFeature,
                private readonly htmlFeature: RazorHtmlFeature,
                private readonly serviceClient: RazorLanguageServiceClient) {
    }

    public async provideCompletionItems(
            document: vscode.TextDocument, position: vscode.Position,
            token: vscode.CancellationToken, context: vscode.CompletionContext) {
        const languageResponse = await this.serviceClient.languageQuery(position, document.uri);
        const projectionProvider = languageResponse.kind === LanguageKind.CSharp
            ? this.csharpFeature.projectionProvider
            : this.htmlFeature.projectionProvider;
        const projectedDocument = await projectionProvider.getDocument(document.uri);
        const projectedUri = projectedDocument.projectedUri;

        if (projectedUri) {
            return vscode.commands.executeCommand<vscode.CompletionList | vscode.CompletionItem[]>(
                'vscode.executeCompletionItemProvider',
                projectedUri,
                languageResponse.position,
                context.triggerCharacter);
        }

        return { isIncomplete: false, items: [] } as vscode.CompletionList;
    }
}
