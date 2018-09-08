/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';

import { IProjectedDocument } from './IProjectedDocument';
import { RazorLanguage } from './RazorLanguage';

export class RazorDocumentSynchronizer {
    private readonly synchronizations: { [uri: string]: SynchronizationContext } = {};

    public register() {
        const changeRegistration = vscode.workspace.onDidChangeTextDocument((args) => {
            if (args.document.languageId !== RazorLanguage.id) {
                return;
            }

            const uriPath = this.getUriPath(args.document.uri);
            const context = this.synchronizations[uriPath];

            if (context && args.document.version >= context.documentVersion) {
                context.synchronized(true);
            }
        });

        const closeRegistration = vscode.workspace.onDidCloseTextDocument((document) => {
            if (document.languageId !== RazorLanguage.id) {
                return;
            }

            const uriPath = this.getUriPath(document.uri);
            const context = this.synchronizations[uriPath];

            if (context) {
                context.synchronized(false);
            }
        });

        return vscode.Disposable.from(changeRegistration, closeRegistration);
    }

    public async trySynchronize(
        hostDocument: vscode.TextDocument,
        projectedDocument: IProjectedDocument,
        toVersion: number) {
        if (projectedDocument.hostDocumentSyncVersion === hostDocument.version) {
            // Already synchronized
            return true;
        }

        if (toVersion !== hostDocument.version) {
            // Already out-of-date. Failed to synchronize.
            return false;
        }

        let synchronized: (success: boolean) => void;
        const uriPath = this.getUriPath(hostDocument.uri);

        let synchronizationContext = this.synchronizations[uriPath];
        if (synchronizationContext) {
            // Already a synchronization for this document.

            if (synchronizationContext.toVersion < toVersion) {
                // Currently tracked synchronization is older than the requeseted.
                // Mark old one as failed.
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
            synchronizationContext.synchronized(false);
        }, 500);
        synchronizationContext = {
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
            // Already out-of-date, failed to synchronize.
            return false;
        }

        return true;
    }

    private getUriPath(uri: vscode.Uri) {
        const uriPath = uri.fsPath || uri.path;

        return uriPath;
    }
}

interface SynchronizationContext {
    readonly toVersion: number;
    readonly documentVersion: number;
    readonly synchronized: (success: boolean) => void;
    readonly onSynchronized: Promise<boolean>;
}
