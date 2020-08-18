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
        // This is kind of wrong. We're manually trimming whitespace and adjusting
        // for multi-line edits that're provided by O#. Right now, we do not support multi-line edits
        // (ex. refactoring code actions) however there are certain supported edits which O# is automatically
        // formatting for us (ex. FullyQualifiedNamespace) into multiple lines, when it should span a single line.
        // This is due to how we render our virtual cs files, with fewer levels of indentation to facilitate
        // appropriate error reporting (if we had additional tabs, then the error squigly would appear offset).
        //
        // The ideal solution for this would do something like:
        // https://github.com/dotnet/aspnetcore-tooling/blob/4c8dbd0beb073e6dcee33d96d174453bf44d938a/
        // src/Razor/src/Microsoft.AspNetCore.Razor.LanguageServer/Formatting/DefaultRazorFormattingService.cs#L264
        // however we're going to hold off on that for now as it isn't immediately necessary and we don't
        // (currently) support any other kind of multi-line edits.

        const newText = edit.newText.trim();

        // The starting and ending range may be equal in the case when we have other items on the same line. Ex:
        // Render|Tree apple
        // where `|` is the cursor. We want to ensure we dont't overwrite `apple` in this case with our edit.
        if (newText !== edit.newText && !edit.range.start.isEqual(edit.range.end)) {
            const end = new vscode.Position(edit.range.start.line, edit.range.start.character + newText.length);
            edit.range = new vscode.Range(edit.range.start, end);
        }

        const newEdit = new vscode.TextEdit(edit.range, newText);
        return [ uri, newEdit ];
    }

    public canHandleEdit(uri: vscode.Uri, edit: vscode.TextEdit) {
        // CodeActions do not have a distinct identifier, so we must determine
        // if a potential edit is a Fully Qualified Namespace edit. We do so by
        // examining whether the new text of the edit fits one of two potential forms.
        const completeFullyQualifiedRegex = new RegExp('^(\\w+\\.)+\\w+$'); // Microsoft.AspNetCore.Mvc
        const partialFullyQualifiedRegex = new RegExp('^(\\w+\\.)+$');      // Microsoft.AspNetCore.Mvc.

        const newText = edit.newText.trim();

        return (!edit.range.isSingleLine && completeFullyQualifiedRegex.test(newText)) ||
            (edit.range.start.isEqual(edit.range.end) && partialFullyQualifiedRegex.test(newText));
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
