/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { RazorCSharpFeature } from './CSharp/RazorCSharpFeature';
import { RazorHtmlFeature } from './Html/RazorHtmlFeature';
import { ProjectionResult } from './ProjectionResult';
import { RazorLanguageServiceClient } from './RazorLanguageServiceClient';
import { LanguageKind } from './RPC/LanguageKind';

export class RazorLanguageFeatureBase {
    constructor(
        protected readonly csharpFeature: RazorCSharpFeature,
        protected readonly htmlFeature: RazorHtmlFeature,
        protected readonly serviceClient: RazorLanguageServiceClient) {
    }

    protected async getProjection(document: vscode.TextDocument, position: vscode.Position) {
        const languageResponse = await this.serviceClient.languageQuery(position, document.uri);

        switch (languageResponse.kind) {
            case LanguageKind.CSharp:
            case LanguageKind.Html:
                const projectionProvider = languageResponse.kind === LanguageKind.CSharp
                    ? this.csharpFeature.projectionProvider
                    : this.htmlFeature.projectionProvider;
                const projectedDocument = await projectionProvider.getDocument(document.uri);
                const projectedUri = projectedDocument.projectedUri;

                return {
                    uri: projectedUri,
                    position: languageResponse.position,
                    languageKind: languageResponse.kind,
                } as ProjectionResult;

            default:
                return null;
        }
    }
}
