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
    private readonly _csharpFeature: RazorCSharpFeature;
    private readonly _htmlFeature: RazorHtmlFeature;
    private readonly _serviceClient: RazorLanguageServiceClient;

    constructor(csharpFeature: RazorCSharpFeature, htmlFeature: RazorHtmlFeature, serviceClient: RazorLanguageServiceClient) {
        this._csharpFeature = csharpFeature;
        this._htmlFeature = htmlFeature;
        this._serviceClient = serviceClient;
    }

    public async provideCompletionItems(document: vscode.TextDocument, position: vscode.Position, token: vscode.CancellationToken, context: vscode.CompletionContext): Promise<vscode.CompletionItem[] | vscode.CompletionList> {
        let languageResponse = await this._serviceClient.languageQuery(position, document.uri);
        let projectedUri: vscode.Uri | undefined;

        if (languageResponse.kind === LanguageKind.CSharp) {
            let projectedCSharpDocument = await this._csharpFeature.ProjectionProvider.getDocument(document.uri);
            projectedUri = projectedCSharpDocument.projectedUri;
        }
        else if (languageResponse.kind === LanguageKind.Html) {
            let projectedHtmlDocument = await this._htmlFeature.ProjectionProvider.getDocument(document.uri);
            projectedUri = projectedHtmlDocument.projectedUri;
        }

        let completionList: vscode.CompletionList | vscode.CompletionItem[] | undefined;
        if (projectedUri) {
            completionList = await vscode.commands.executeCommand<vscode.CompletionList | vscode.CompletionItem[]>(
                "vscode.executeCompletionItemProvider",
                projectedUri,
                languageResponse.position,
                context.triggerCharacter);
        }

        if (!completionList) {
            completionList = {
                isIncomplete: false,
                items: []
            };
        }

        return completionList;
    }

}