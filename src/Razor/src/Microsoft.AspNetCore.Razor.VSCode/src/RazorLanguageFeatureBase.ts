/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { ProjectionResult } from './ProjectionResult';
import { RazorDocumentManager } from './RazorDocumentManager';
import { RazorDocumentSynchronizer } from './RazorDocumentSynchronizer';
import { RazorLanguageServiceClient } from './RazorLanguageServiceClient';
import { RazorLogger } from './RazorLogger';
import { LanguageKind } from './RPC/LanguageKind';
import { getUriPath } from './UriPaths';

export class RazorLanguageFeatureBase {
    constructor(
        private readonly documentSynchronizer: RazorDocumentSynchronizer,
        protected readonly documentManager: RazorDocumentManager,
        protected readonly serviceClient: RazorLanguageServiceClient,
        protected readonly logger: RazorLogger) {
    }

    protected async getProjection(
        document: vscode.TextDocument,
        position: vscode.Position,
        token: vscode.CancellationToken) {
        const languageResponse = await this.serviceClient.languageQuery(position, document.uri);

        switch (languageResponse.kind) {
            case LanguageKind.CSharp:
            case LanguageKind.Html:
                const razorDocument = await this.documentManager.getDocument(document.uri);
                const projectedDocument = languageResponse.kind === LanguageKind.CSharp
                    ? razorDocument.csharpDocument
                    : razorDocument.htmlDocument;

                if (languageResponse.hostDocumentVersion === undefined) {
                    // There should always be a document version attached to an open document.
                    // Log it and move on as if it was synchronized.
                    if (this.logger.verboseEnabled) {
                        this.logger.logVerbose(
                            `Could not find a document version associated with the document '${getUriPath(document.uri)}'.`);
                    }
                } else {
                    const synchronized = await this.documentSynchronizer.trySynchronizeProjectedDocument(
                        document,
                        projectedDocument,
                        languageResponse.hostDocumentVersion,
                        token);
                    if (!synchronized) {
                        // Could not synchronize
                        return null;
                    }
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
