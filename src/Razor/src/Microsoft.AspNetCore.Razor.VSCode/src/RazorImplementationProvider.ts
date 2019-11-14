/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { virtualCSharpSuffix, virtualHtmlSuffix } from './RazorDocumentFactory';
import { RazorLanguageFeatureBase } from './RazorLanguageFeatureBase';
import { LanguageKind } from './RPC/LanguageKind';
import { getUriPath } from './UriPaths';

export class RazorImplementationProvider
    extends RazorLanguageFeatureBase
    implements vscode.ImplementationProvider {

    public async provideImplementation(
        document: vscode.TextDocument,
        position: vscode.Position,
        token: vscode.CancellationToken) {
        const projection = await this.getProjection(document, position, token);
        if (!projection) {
            return;
        }

        const implementations = await vscode.commands.executeCommand<vscode.Definition>(
            'vscode.executeImplementationProvider',
            projection.uri,
            projection.position) as vscode.Location[];

        if (projection.languageKind === LanguageKind.CSharp) {
            // C# knows about line pragma, if we're getting a direction to a virtual c# document
            // that means the piece we're trying to navigate to does not have a representation in the
            // top level file.
            for (const implementation of implementations) {
                const uriPath = getUriPath(implementation.uri);
                if (uriPath.endsWith(virtualCSharpSuffix)) {
                    const modifiedPath = uriPath.replace(virtualCSharpSuffix, '');
                    const modifiedFile = vscode.Uri.file(modifiedPath);
                    const res = await this.serviceClient.mapToDocumentRange(
                        projection.languageKind,
                        implementation.range,
                        modifiedFile);
                    implementation.range = res.range;
                    implementation.uri = modifiedFile;
                }
            }
        }

        if (projection.languageKind === LanguageKind.Html) {
            // We don't think that javascript implementations are supported by VSCodes HTML support.
            // Since we shim through to them we'll do nothing until we get an ask.
            return;
        }

        return implementations;
    }
}
