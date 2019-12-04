/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { getRazorDocumentUri, isRazorCSharpFile } from './RazorConventions';
import { RazorLanguageFeatureBase } from './RazorLanguageFeatureBase';
import { LanguageKind } from './RPC/LanguageKind';

export class RazorDefinitionProvider
    extends RazorLanguageFeatureBase
    implements vscode.DefinitionProvider {

    public async provideDefinition(
        document: vscode.TextDocument, position: vscode.Position,
        token: vscode.CancellationToken) {

        const projection = await this.getProjection(document, position, token);
        if (!projection) {
            return;
        }

        const definitions = await vscode.commands.executeCommand<vscode.Definition>(
            'vscode.executeDefinitionProvider',
            projection.uri,
            projection.position) as vscode.Location[];

        if (projection.languageKind === LanguageKind.CSharp) {
            for (const definition of definitions) {
                if (!isRazorCSharpFile(definition.uri)) {
                    // This is a regular C# file. No need to re-map.
                    continue;
                }

                // C# knows about line pragma, if we're getting a direction to a virtual c# document
                // that means the piece we're trying to navigate to may not have a representation in the
                // top level file.
                const razorFile = getRazorDocumentUri(definition.uri);
                const result = await this.serviceClient.mapToDocumentRange(
                    projection.languageKind,
                    definition.range,
                    razorFile);

                if (result) {
                    definition.range = result!.range;
                    definition.uri = razorFile;
                }
            }
        }

        if (projection.languageKind === LanguageKind.Html) {
            definitions.forEach(definition => {
                // Because the line pragmas for html are generated referencing the projected document
                // we need to remap their file locations to reference the top level Razor document.
                const razorFile = getRazorDocumentUri(definition.uri);
                definition.uri = razorFile;
            });
        }

        return definitions;
    }
}
