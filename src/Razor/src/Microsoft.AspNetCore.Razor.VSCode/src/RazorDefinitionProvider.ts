/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { backgroundVirtualCSharpSuffix, virtualCSharpSuffix, virtualHtmlSuffix } from './RazorDocumentFactory';
import { RazorLanguageFeatureBase } from './RazorLanguageFeatureBase';
import { LanguageKind } from './RPC/LanguageKind';
import { getUriPath } from './UriPaths';

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
            // C# knows about line pragma, if we're getting a direction to a virtual c# document
            // that means the piece we're trying to navigate to may not have a representation in the
            // top level file.
            for (const definition of definitions) {
                const uriPath = getUriPath(definition.uri);
                if (uriPath.endsWith(virtualCSharpSuffix)) {
                    // The virtual file is named differently if it's not open (in the background)
                    let razorFilePath = uriPath.replace(backgroundVirtualCSharpSuffix, '');
                    razorFilePath = uriPath.replace(virtualCSharpSuffix, '');
                    const razorFile = vscode.Uri.file(razorFilePath);
                    const res = await this.serviceClient.mapToDocumentRange(
                        projection.languageKind,
                        definition.range,
                        razorFile);
                    if (!res) {
                        definition.range = res!.range;
                        definition.uri = razorFile;
                    }
                }
            }
        }

        if (projection.languageKind === LanguageKind.Html) {
            definitions.forEach(definition => {
                // Because the line pragmas for html are generated referencing the projected document
                // we need to remap their file locations to reference the top level Razor document.
                const uriPath = getUriPath(definition.uri);
                const path = uriPath.replace(virtualHtmlSuffix, '');
                definition.uri = vscode.Uri.file(path);
            });
        }

        return definitions;
    }
}
