/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { RazorLanguageServerClient } from '../RazorLanguageServerClient';
import { RazorLogger } from '../RazorLogger';
import { RazorCodeAction } from '../RPC/RazorCodeAction';
import { RazorCodeActionResolutionParams } from '../RPC/RazorCodeActionResolutionParams';
import { convertWorkspaceEditFromSerializable } from '../RPC/SerializableWorkspaceEdit';

export class RazorCodeActionRunner {
    private static readonly codeActionResolveEndpoint = 'textDocument/codeActionResolve';
    private static readonly razorCodeActionRunnerCommand = 'razor/runCodeAction';

    constructor(
        private readonly serverClient: RazorLanguageServerClient,
        private readonly logger: RazorLogger,
    ) {}

    public register(): vscode.Disposable {
        return vscode.commands.registerCommand(
            RazorCodeActionRunner.razorCodeActionRunnerCommand,
            (request: RazorCodeActionResolutionParams) => this.runCodeAction(request),
            this);
    }

    private async runCodeAction(request: RazorCodeActionResolutionParams): Promise<boolean> {
        const response: RazorCodeAction = await this.serverClient.sendRequest(
            RazorCodeActionRunner.codeActionResolveEndpoint,
            { data: request, title: request.action });

        let changesWorkspaceEdit: vscode.WorkspaceEdit;
        let documentChangesWorkspaceEdit: vscode.WorkspaceEdit;

        try {
            changesWorkspaceEdit = convertWorkspaceEditFromSerializable({changes: response.edit.changes});
            documentChangesWorkspaceEdit = convertWorkspaceEditFromSerializable({documentChanges: response.edit.documentChanges});
        } catch (error) {
            this.logger.logError(`Unexpected error deserializing code action for ${request.action}`, error);
            return Promise.resolve(false);
        }

        return vscode.workspace.applyEdit(documentChangesWorkspaceEdit).then(() => vscode.workspace.applyEdit(changesWorkspaceEdit));
    }
}
