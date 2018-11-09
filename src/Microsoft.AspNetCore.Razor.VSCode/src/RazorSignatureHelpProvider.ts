/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { RazorLanguageFeatureBase } from './RazorLanguageFeatureBase';

export class RazorSignatureHelpProvider
    extends RazorLanguageFeatureBase
    implements vscode.SignatureHelpProvider {

    public async provideSignatureHelp(
        document: vscode.TextDocument, position: vscode.Position,
        token: vscode.CancellationToken) {

        const projection = await this.getProjection(document, position, token);
        if (projection) {
            const result = await vscode.commands.executeCommand<vscode.SignatureHelp>(
                    'vscode.executeSignatureHelpProvider',
                    projection.uri,
                    projection.position);
            return result;
        }
    }
}
