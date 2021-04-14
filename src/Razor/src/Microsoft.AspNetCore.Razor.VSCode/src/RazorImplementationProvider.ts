/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { RazorLanguageFeatureBase } from './RazorLanguageFeatureBase';
import { LanguageKind } from './RPC/LanguageKind';

export class RazorImplementationProvider
    extends RazorLanguageFeatureBase
    implements vscode.ImplementationProvider {

    public async provideImplementation(
        document: vscode.TextDocument,
        position: vscode.Position,
        token: vscode.CancellationToken) {

        const projection = await this.getProjection(document, position, token);
        if (!projection ||
            projection.languageKind === LanguageKind.Html ||
            projection.languageKind === LanguageKind.Razor) {
            // We don't think that javascript implementations are supported by VSCodes HTML support.
            // Since we shim through to them we'll do nothing until we get an ask.
            return;
        }

        const implementations = await vscode.commands.executeCommand<vscode.Definition>(
            'vscode.executeImplementationProvider',
            projection.uri,
            projection.position) as vscode.Location[];

        // Omnisharp should have already done all the remapping. Nothing for us to do here.
        return implementations;
    }
}
