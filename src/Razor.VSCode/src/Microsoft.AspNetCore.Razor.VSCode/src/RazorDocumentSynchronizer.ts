/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';

import { CSharpProjectedDocumentContentProvider } from './CSharp/CSharpProjectedDocumentContentProvider';
import { HtmlProjectedDocumentContentProvider } from './Html/HtmlProjectedDocumentContentProvider';
import { IProjectedDocument } from './IProjectedDocument';
import { IRazorDocumentChangeEvent } from './IRazorDocumentChangeEvent';
import { RazorDocumentChangeKind } from './RazorDocumentChangeKind';
import { RazorDocumentManager } from './RazorDocumentManager';
import { RazorLogger } from './RazorLogger';
import { getUriPath } from './UriPaths';

export class RazorDocumentSynchronizer {
    private readonly synchronizations: { [uri: string]: SynchronizationContext[] } = {};
    private synchronizationIdentifier = 0;

    constructor(
        private readonly documentManager: RazorDocumentManager,
        private readonly logger: RazorLogger) {
    }

    public register() {
        const documentManagerRegistration = this.documentManager.onChange(
            event => this.documentChanged(event));
        const textDocumentChangeRegistration = vscode.workspace.onDidChangeTextDocument(
            event => this.textDocumentChanged(event));

        return vscode.Disposable.from(documentManagerRegistration, textDocumentChangeRegistration);
    }

    public async trySynchronizeProjectedDocument(
        hostDocument: vscode.TextDocument,
        projectedDocument: IProjectedDocument,
        expectedHostDocumentVersion: number,
        token: vscode.CancellationToken) {

        const logId = ++this.synchronizationIdentifier;

        const documentKey = getUriPath(projectedDocument.uri);
        if (this.logger.verboseEnabled) {
            const ehdv = expectedHostDocumentVersion;
            this.logger.logVerbose(
                `${logId} - Synchronizing '${documentKey}':
    Currently at ${projectedDocument.hostDocumentSyncVersion}, synchronizing to version '${ehdv}'.
    Current host document version: '${hostDocument.version}'
    Current projected document version: '${projectedDocument.projectedDocumentSyncVersion}'`);
        }

        if (hostDocument.version !== expectedHostDocumentVersion) {
            if (this.logger.verboseEnabled) {
                this.logger.logVerbose(
                    `${logId} - toHostDocumentVersion and hostDocument.version already out of date.`);
            }

            // Already out-of-date. Allowing synchronizations for now to see if this actually causes any issues.
        }

        const context: SynchronizationContext = this.createSynchronizationContext(
            documentKey,
            projectedDocument,
            expectedHostDocumentVersion,
            hostDocument,
            token);

        try {
            if (projectedDocument.hostDocumentSyncVersion !== expectedHostDocumentVersion) {
                if (this.logger.verboseEnabled) {
                    this.logger.logVerbose(
                        `${logId} - Projected document not in sync with host document, waiting for update...
    Current host document sync version: ${projectedDocument.hostDocumentSyncVersion}`);
                }
                await context.onProjectedDocumentSynchronized;
            }

            if (this.logger.verboseEnabled) {
                this.logger.logVerbose(
                    `${logId} - Projected document in sync with host document`);
            }

            // Projected document is the one we expect.

            const projectedTextDocument = await vscode.workspace.openTextDocument(projectedDocument.uri);
            const projectedTextDocumentVersion = this.getProjectedTextDocumentVersion(projectedTextDocument);
            if (projectedDocument.projectedDocumentSyncVersion !== projectedTextDocumentVersion) {
                if (this.logger.verboseEnabled) {
                    this.logger.logVerbose(
                        `${logId} - Projected text document not in sync with data type, waiting for update...
    Current projected text document sync version: ${projectedTextDocumentVersion}`);
                }
                await context.onProjectedTextDocumentSynchronized;
            }

            if (this.logger.verboseEnabled) {
                this.logger.logVerbose(
                    `${logId} - Projected text document in sync with data type`);
            }

            // Projected text document is the one we expect
        } catch (cancellationReason) {
            this.removeSynchronization(context);

            if (this.logger.verboseEnabled) {
                this.logger.logVerbose(
                    `${logId} - Synchronization failed: ${cancellationReason}`);
            }

            return false;
        }

        this.removeSynchronization(context);

        if (this.logger.verboseEnabled) {
            this.logger.logVerbose(
                `${logId} - Synchronization successful!`);
        }

        return true;
    }

    private removeSynchronization(context: SynchronizationContext) {
        const documentKey = getUriPath(context.projectedDocument.uri);
        const synchronizations = this.synchronizations[documentKey];
        clearTimeout(context.timeoutId);

        if (synchronizations.length === 1) {
            delete this.synchronizations[documentKey];
            return;
        }

        this.synchronizations[documentKey] = synchronizations.filter(item => item !== context);
    }

    private createSynchronizationContext(
        documentKey: string,
        projectedDocument: IProjectedDocument,
        toHostDocumentVersion: number,
        hostDocument: vscode.TextDocument,
        token: vscode.CancellationToken) {

        const rejectionsForCancel: Array<(reason: string) => void> = [];
        let projectedDocumentSynchronized: () => void = Function;
        const onProjectedDocumentSynchronized = new Promise<void>((resolve, reject) => {
            projectedDocumentSynchronized = resolve;
            rejectionsForCancel.push(reject);
        });
        let projectedTextDocumentSynchronized: () => void = Function;
        const onProjectedTextDocumentSynchronized = new Promise<void>((resolve, reject) => {
            projectedTextDocumentSynchronized = resolve;
            rejectionsForCancel.push(reject);
        });

        token.onCancellationRequested((reason) => {
            context.cancel(`Token cancellation requested: ${reason}`);
        });
        const timeoutId = setTimeout(() => {
            context.cancel('Synchronization timed out');
        }, 2000);
        const context: SynchronizationContext = {
            projectedDocument,
            logIdentifier: this.synchronizationIdentifier,
            timeoutId,
            toHostDocumentVersion,
            hostDocumentVersion: hostDocument.version,
            cancel: (reason) => {
                for (const reject of rejectionsForCancel) {
                    reject(reason);
                }
            },
            projectedDocumentSynchronized,
            onProjectedDocumentSynchronized,
            projectedTextDocumentSynchronized,
            onProjectedTextDocumentSynchronized,
        };

        let synchronizations = this.synchronizations[documentKey];
        if (!synchronizations) {
            synchronizations = [];
            this.synchronizations[documentKey] = synchronizations;
        }

        synchronizations.push(context);

        return context;
    }

    private textDocumentChanged(event: vscode.TextDocumentChangeEvent) {
        if (event.document.uri.scheme !== CSharpProjectedDocumentContentProvider.scheme &&
            event.document.uri.scheme !== HtmlProjectedDocumentContentProvider.scheme) {
            return;
        }

        const projectedTextDocumentVersion = this.getProjectedTextDocumentVersion(event.document);
        if (projectedTextDocumentVersion === null) {
            return;
        }

        const documentKey = getUriPath(event.document.uri);
        const synchronizationContexts = this.synchronizations[documentKey];

        if (!synchronizationContexts) {
            return;
        }

        for (const context of synchronizationContexts) {
            if (context.projectedDocument.projectedDocumentSyncVersion === projectedTextDocumentVersion) {
                if (this.logger.verboseEnabled) {
                    const li = context.logIdentifier;
                    const ptdv = projectedTextDocumentVersion;
                    this.logger.logVerbose(`${li} - Projected text document synchronized to ${ptdv}.`);
                }
                context.projectedTextDocumentSynchronized();
            }
        }
    }

    private documentChanged(event: IRazorDocumentChangeEvent) {
        let projectedDocument: IProjectedDocument;
        if (event.kind === RazorDocumentChangeKind.csharpChanged) {
            projectedDocument = event.document.csharpDocument;
        } else if (event.kind === RazorDocumentChangeKind.htmlChanged) {
            projectedDocument = event.document.htmlDocument;
        } else {
            return;
        }

        const hostDocumentSyncVersion = projectedDocument.hostDocumentSyncVersion;
        if (hostDocumentSyncVersion === null) {
            return;
        }

        const documentKey = getUriPath(projectedDocument.uri);
        const synchronizationContexts = this.synchronizations[documentKey];
        if (!synchronizationContexts) {
            return;
        }

        for (const context of synchronizationContexts) {
            if (context.toHostDocumentVersion === projectedDocument.hostDocumentSyncVersion) {
                context.projectedDocumentSynchronized();
            }
        }
    }

    private getProjectedTextDocumentVersion(textDocument: vscode.TextDocument) {
        // Logic defined in this method is heavily dependent on the functionality in the projected
        // document content providers to append versions to the end of text documents.

        if (textDocument.lineCount <= 0) {
            return null;
        }

        const lastLine = textDocument.lineAt(textDocument.lineCount - 1);
        const versionString = lastLine.text.substring(3 /* //_ */);
        const textDocumentProjectedVersion = parseInt(versionString, 10);

        return textDocumentProjectedVersion;
    }
}

interface SynchronizationContext {
    readonly projectedDocument: IProjectedDocument;
    readonly logIdentifier: number;
    readonly toHostDocumentVersion: number;
    readonly hostDocumentVersion: number;
    readonly timeoutId: NodeJS.Timer;
    readonly projectedDocumentSynchronized: () => void;
    readonly onProjectedDocumentSynchronized: Promise<void>;
    readonly projectedTextDocumentSynchronized: () => void;
    readonly onProjectedTextDocumentSynchronized: Promise<void>;
    readonly cancel: (reason: string) => void;
}
