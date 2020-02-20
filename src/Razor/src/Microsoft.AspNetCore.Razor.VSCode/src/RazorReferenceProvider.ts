/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { getRazorDocumentUri, isRazorHtmlFile } from './RazorConventions';
import { RazorLanguageFeatureBase } from './RazorLanguageFeatureBase';
import { LanguageKind } from './RPC/LanguageKind';

export class RazorReferenceProvider
    extends RazorLanguageFeatureBase
    implements vscode.ReferenceProvider {

    public async provideReferences(
        document: vscode.TextDocument,
        position: vscode.Position,
        context: vscode.ReferenceContext,
        token: vscode.CancellationToken) {

        const projection = await this.getProjection(document, position, token);
        if (!projection || projection.languageKind === LanguageKind.Razor) {
            return;
        }

        const references = await vscode.commands.executeCommand<vscode.Location[]>(
            'vscode.executeReferenceProvider',
            projection.uri,
            projection.position) as vscode.Location[];

        const result = new Array<vscode.Location>();
        for (const reference of references) {
            if (projection.languageKind === LanguageKind.Html && isRazorHtmlFile(reference.uri)) {

                // Because the line pragmas for html are generated referencing the projected document
                // we need to remap their file locations to reference the top level Razor document.
                const razorFile = getRazorDocumentUri(reference.uri);
                result.push(new vscode.Location(razorFile, reference.range));

            } else {
                // This means it is one of the following,
                // 1. A .razor/.cshtml file (because OmniSharp already remapped the background C# to the original document)
                // 2. A .cs file
                // 3. A .html/.js file
                // In all of these cases, we don't need to remap. So accept it as is and move on.
                result.push(reference);
            }
        }

        return result;
    }
}
