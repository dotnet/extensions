/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { ProjectionResult } from './ProjectionResult';
import { RazorDocumentManager } from './RazorDocumentManager';
import { RazorDocumentSynchronizer } from './RazorDocumentSynchronizer';
import { RazorLanguageServiceClient } from './RazorLanguageServiceClient';
import { LanguageKind } from './RPC/LanguageKind';

export class RazorLanguageFeatureBase {
    constructor(
        private readonly documentSynchronizer: RazorDocumentSynchronizer,
        protected readonly documentManager: RazorDocumentManager,
        protected readonly serviceClient: RazorLanguageServiceClient) {
    }

    protected async getProjection(document: vscode.TextDocument, position: vscode.Position) {
        const languageResponse = await this.serviceClient.languageQuery(position, document.uri);

        switch (languageResponse.kind) {
            case LanguageKind.CSharp:
            case LanguageKind.Html:
                const razorDocument = await this.documentManager.getDocument(document.uri);
                const projectedDocument = languageResponse.kind === LanguageKind.CSharp
                    ? razorDocument.csharpDocument
                    : razorDocument.htmlDocument;

                const synchronized = await this.documentSynchronizer.trySynchronize(
                    document,
                    projectedDocument,
                    languageResponse.hostDocumentVersion);
                if (!synchronized) {
                    // Could not synchronize
                    return null;
                }

                const projectedUri = projectedDocument.uri;
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
