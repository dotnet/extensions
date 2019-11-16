/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { backgroundVirtualCSharpSuffix, virtualCSharpSuffix, virtualHtmlSuffix } from './RazorDocumentFactory';
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

        let implementations = await vscode.commands.executeCommand<vscode.Definition>(
            'vscode.executeImplementationProvider',
            projection.uri,
            projection.position) as vscode.Location[];

        if (projection.languageKind === LanguageKind.CSharp) {
            // C# knows about line pragma, if we're getting a direction to a virtual c# document
            // that means the piece we're trying to navigate to may not have a representation in the
            // top level file.
            for (const implementation of implementations) {
                const uriPath = getUriPath(implementation.uri);
                if (uriPath.endsWith(virtualCSharpSuffix)) {
                    // The virtual file is named differently if it's not open (in the background)
                    let razorFilePath = uriPath.replace(backgroundVirtualCSharpSuffix, '');
                    razorFilePath = uriPath.replace(virtualCSharpSuffix, '');
                    const razorFile = vscode.Uri.file(razorFilePath);
                    const res = await this.serviceClient.mapToDocumentRange(
                        projection.languageKind,
                        implementation.range,
                        razorFile);
                    if (res) {
                        implementation.range = res!.range;
                        implementation.uri = razorFile;
                    }
                }
            }
        }

        if (projection.languageKind === LanguageKind.Html) {
            // We don't think that javascript implementations are supported by VSCodes HTML support.
            // Since we shim through to them we'll do nothing until we get an ask.
            return;
        }

        // Remove any non-top level Razor file implementation entries. We don't know how to navigate to them.
        implementations = implementations.filter(async (impl) => {
            const uriPath = getUriPath(impl.uri);
            return !uriPath.endsWith(virtualCSharpSuffix);
        });

        return implementations;
    }
}
