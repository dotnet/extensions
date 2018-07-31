/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { RazorCSharpFeature } from './CSharp/RazorCSharpFeature';
import { RazorHtmlFeature } from './Html/RazorHtmlFeature';

export class RazorCompletionItemProvider implements vscode.CompletionItemProvider {
    private _csharpFeature: RazorCSharpFeature;
    private _htmlFeature: RazorHtmlFeature;

    constructor(csharpFeature: RazorCSharpFeature, htmlFeature: RazorHtmlFeature) {
        this._csharpFeature = csharpFeature;
        this._htmlFeature = htmlFeature;
    }

    public async provideCompletionItems(document: vscode.TextDocument, position: vscode.Position, token: vscode.CancellationToken, context: vscode.CompletionContext): Promise<vscode.CompletionItem[] | vscode.CompletionList> {
        let projectedDocument = await this._csharpFeature.ProjectionProvider.getDocument(document.uri);
        let completionList = await vscode.commands.executeCommand<vscode.CompletionList | vscode.CompletionItem[]>(
            "vscode.executeCompletionItemProvider",
            projectedDocument.projectedUri,
            position,
            context.triggerCharacter);

        if (!completionList) {
            completionList = {
                isIncomplete: true,
                items: []
            };
        }

        return completionList;
    }

}