/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';

import { IProjectedDocument } from './IProjectedDocument';
import { IRazorDocumentChangeEvent } from './IRazorDocumentChangeEvent';
import { RazorDocumentChangeKind } from './RazorDocumentChangeKind';
import { RazorDocumentManager } from './RazorDocumentManager';
import { RazorLanguage } from './RazorLanguage';
import { RazorLogger } from './RazorLogger';
import { getUriPath } from './UriPaths';

export class RazorDocumentSynchronizer {
    private readonly synchronizations: { [uri: string]: SynchronizationContext } = {};
    private synchronizationIdentifier = 0;

    constructor(
        documentManager: RazorDocumentManager,
        private readonly logger: RazorLogger) {
        documentManager.onChange((event) => this.documentChanged(event));
    }

    public register() {
        const changeRegistration = vscode.workspace.onDidChangeTextDocument((args) => {
            if (args.document.languageId !== RazorLanguage.id) {
                return;
            }

            const uriPath = getUriPath(args.document.uri);
            const context = this.synchronizations[uriPath];

            if (context && args.document.version >= context.documentVersion) {
                if (this.logger.verboseEnabled) {
                    this.logger.logVerbose(
                        `${context.logIdentifier} - Notify Success: Host document updated to equivalent version.`);
                }
                context.synchronized(true);
            }
        });

        return changeRegistration;
    }

    public async trySynchronize(
        hostDocument: vscode.TextDocument,
        projectedDocument: IProjectedDocument,
        toVersion: number) {
        const logIdentifier = this.synchronizationIdentifier++;

        if (this.logger.verboseEnabled) {
            this.logger.logVerbose(`${logIdentifier} - Synchronizing '${getUriPath(projectedDocument.uri)}' ` +
                `currently at ${projectedDocument.hostDocumentSyncVersion} to version '${toVersion}'. ` +
                `Current host document version: '${hostDocument.version}'`);
        }

        if (projectedDocument.hostDocumentSyncVersion === hostDocument.version) {
            if (this.logger.verboseEnabled) {
                this.logger.logVerbose(
                    `${logIdentifier} - Success: Projected document and host document already synchronized.`);
            }

            // Already synchronized
            return true;
        }

        if (toVersion !== hostDocument.version) {
            if (this.logger.verboseEnabled) {
                this.logger.logVerbose(
                    `${logIdentifier} - Failed: toVersion and host document version already out of date.`);
            }

            // Already out-of-date. Failed to synchronize.
            return false;
        }

        let synchronized: (success: boolean) => void;
        const uriPath = getUriPath(hostDocument.uri);

        let synchronizationContext = this.synchronizations[uriPath];
        if (synchronizationContext) {
            // Already a synchronization for this document.

            if (synchronizationContext.toVersion < toVersion) {
                // Currently tracked synchronization is older than the requeseted.
                // Mark old one as failed.
                if (this.logger.verboseEnabled) {
                    this.logger.logVerbose(`${logIdentifier} - Notify Failed: Newer synchronization request came in.`);
                }
                synchronizationContext.synchronized(false);
            } else {
                // The already tracked synchronization is sufficient.
                return synchronizationContext.onSynchronized;
            }
        }

        const onSynchronized = new Promise<boolean>((resolve) => {
            synchronized = resolve;
        });
        const timeout = setTimeout(() => {
            if (this.logger.verboseEnabled) {
                this.logger.logVerbose(`${logIdentifier} - Notify Failed: Synchronization timed out.`);
            }
            synchronizationContext.synchronized(false);
        }, 500);
        synchronizationContext = {
            logIdentifier,
            toVersion,
            documentVersion: hostDocument.version,
            synchronized: (s) => {
                delete this.synchronizations[uriPath];
                clearTimeout(timeout);
                synchronized(s);
            },
            onSynchronized,
        };
        this.synchronizations[uriPath] = synchronizationContext;

        const success = await onSynchronized;

        if (success && projectedDocument.hostDocumentSyncVersion !== hostDocument.version) {
            if (this.logger.verboseEnabled) {
                this.logger.logVerbose(`${logIdentifier} - Failed: User moved on.`);
            }

            // Already out-of-date, failed to synchronize.
            return false;
        }

        if (success) {
            if (this.logger.verboseEnabled) {
                this.logger.logVerbose(`${logIdentifier} - Success: Documents synchronized to version ${toVersion}.`);
            }

            return true;
        } else {
            if (this.logger.verboseEnabled) {
                this.logger.logVerbose(`${logIdentifier} - Failed: Documents not synchronized ${toVersion}.`);
            }

            return false;
        }
    }

    private documentChanged(event: IRazorDocumentChangeEvent) {
        if (event.kind === RazorDocumentChangeKind.csharpChanged) {
            const uriPath = getUriPath(event.document.uri);
            const context = this.synchronizations[uriPath];
            const csharpDocument = event.document.csharpDocument;

            if (csharpDocument.hostDocumentSyncVersion === null) {
                return;
            }

            if (context && csharpDocument.hostDocumentSyncVersion >= context.documentVersion) {
                if (this.logger.verboseEnabled) {
                    this.logger.logVerbose(
                        `${context.logIdentifier} - Notify Success: CSharp updated to ${context.documentVersion}.`);
                }
                context.synchronized(true);
            }
        } else if (event.kind === RazorDocumentChangeKind.closed) {
            const uriPath = getUriPath(event.document.uri);
            const context = this.synchronizations[uriPath];

            if (context) {
                if (this.logger.verboseEnabled) {
                    this.logger.logVerbose(
                        `${context.logIdentifier} - Notify Failed: Document closed.`);
                }

                context.synchronized(false);
            }

        }
    }
}

interface SynchronizationContext {
    readonly logIdentifier: number;
    readonly toVersion: number;
    readonly documentVersion: number;
    readonly synchronized: (success: boolean) => void;
    readonly onSynchronized: Promise<boolean>;
}
