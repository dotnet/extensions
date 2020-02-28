/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { IRazorCodeActionTranslator } from './IRazorCodeActionTranslator';

export class RazorFullyQualifiedCodeActionTranslator implements IRazorCodeActionTranslator {

    private static expectedCode = 'CS0246';

    public applyEdit(
        uri: vscode.Uri,
        edit: vscode.TextEdit): [vscode.Uri | undefined, vscode.TextEdit | undefined] {
        // The edit for this should just translate without additional help.
        throw new Error('Method not implemented.');
    }

    public canHandleEdit(uri: vscode.Uri, edit: vscode.TextEdit): boolean {
        // The edit for this should just translate without additional help.
        return false;
    }

    public canHandleCodeAction(
        codeAction: vscode.Command,
        codeContext: vscode.CodeActionContext,
        document: vscode.TextDocument): boolean {
        const isMissingDiag = (value: vscode.Diagnostic) => {
            return value.severity === vscode.DiagnosticSeverity.Error &&
                value.code === RazorFullyQualifiedCodeActionTranslator.expectedCode;
        };

        const diagnostic = codeContext.diagnostics.find(isMissingDiag);
        if (!diagnostic) {
            return false;
        }

        const codeRange = diagnostic.range;
        const associatedValue = document.getText(codeRange);

        // Once we have the correct diagnostic we want to make sure we have the correct CodeAction.
        // Unfortunately there's no ID for CodeActions, so we have to examine the human-readable title.
        // The title for the fully-qualified code action is just the fully qualified type, so we just ensure
        // 1. It's not a sentence (no spaces).
        // 2. It ends with the short TypeName (so it at least COULD be the Fully-Qualified type).
        if (codeAction.arguments &&
            !codeAction.title.includes(' ') &&
            codeAction.title.endsWith(associatedValue)) {
            return true;
        }

        return false;
    }
}
