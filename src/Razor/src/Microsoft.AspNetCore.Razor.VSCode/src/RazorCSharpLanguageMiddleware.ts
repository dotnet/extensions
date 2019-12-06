/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { getRazorDocumentUri, isRazorCSharpFile } from './RazorConventions';
import { RazorLanguageServiceClient } from './RazorLanguageServiceClient';
import { RazorLogger } from './RazorLogger';
import { LanguageKind } from './RPC/LanguageKind';

// This interface should exactly match the `LanguageMiddleware` interface defined in Omnisharp.
// https://github.com/OmniSharp/omnisharp-vscode/blob/master/src/omnisharp/LanguageMiddlewareFeature.ts#L9-L16
interface LanguageMiddleware {

    language: string;

    remapWorkspaceEdit?(workspaceEdit: vscode.WorkspaceEdit, token: vscode.CancellationToken): vscode.ProviderResult<vscode.WorkspaceEdit>;

    remapLocations?(locations: vscode.Location[], token: vscode.CancellationToken): vscode.ProviderResult<vscode.Location[]>;
}

export class RazorCSharpLanguageMiddleware implements LanguageMiddleware {
    public readonly language = 'Razor';

    constructor(
        private readonly serviceClient: RazorLanguageServiceClient,
        private readonly logger: RazorLogger) {}

    public async remapWorkspaceEdit(workspaceEdit: vscode.WorkspaceEdit, token: vscode.CancellationToken) {
        const result = new vscode.WorkspaceEdit();

        // The returned edits will be for the projected C# documents. We now need to re-map that to the original document.
        for (const entry of workspaceEdit.entries()) {
            const uri = entry[0];
            const edits = entry[1];

            if (!isRazorCSharpFile(uri)) {
                // This edit happens outside of a Razor document. Let the edit go through as is.
                result.set(uri, edits);
                continue;
            }

            // We're now working with a Razor CSharp document.
            const documentUri = getRazorDocumentUri(uri);
            const newEdits = new Array<vscode.TextEdit>();

            // Re-map each edit to its position in the original Razor document.
            for (const edit of edits) {
                const remappedResponse = await this.serviceClient.mapToDocumentRange(
                    LanguageKind.CSharp,
                    edit.range,
                    documentUri);

                if (!remappedResponse || !remappedResponse.range) {
                    // Something went wrong when re-mapping to the original document. Ignore this edit.
                    this.logger.logWarning(`Unable to remap file ${uri.path} at ${edit.range}.`);
                    continue;
                }

                const newEdit = new vscode.TextEdit(remappedResponse.range, edit.newText);
                newEdits.push(newEdit);

                this.logger.logVerbose(
                    `Re-mapping text ${edit.newText} at ${edit.range} in ${uri.path} to ${remappedResponse.range} in ${documentUri.path}`);
            }

            result.set(documentUri, newEdits);
        }

        return result;
    }

    public async remapLocations(locations: vscode.Location[], token: vscode.CancellationToken) {
        const result: vscode.Location[] = [];

        for (const location of locations) {
            if (!isRazorCSharpFile(location.uri)) {
                // This location exists outside of a Razor document. Leave it unchanged.
                result.push(location);
                continue;
            }

            // We're now working with a Razor CSharp document.
            const documentUri = getRazorDocumentUri(location.uri);
            const remappedResponse = await this.serviceClient.mapToDocumentRange(
                LanguageKind.CSharp,
                location.range,
                documentUri);

            if (!remappedResponse || !remappedResponse.range) {
                // Something went wrong when re-mapping to the original document. Ignore this location.
                this.logger.logWarning(`Unable to remap file ${location.uri.path} at ${location.range}.`);
                continue;
            }

            const newLocation = new vscode.Location(documentUri, remappedResponse.range);
            result.push(newLocation);

            this.logger.logVerbose(
                `Re-mapping location ${location.range} in ${location.uri.path} to ${remappedResponse.range} in ${documentUri.path}`);
        }

        return result;
    }

}
