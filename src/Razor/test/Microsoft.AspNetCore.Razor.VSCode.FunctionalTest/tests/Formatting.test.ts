/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as assert from 'assert';
import { afterEach, before, beforeEach } from 'mocha';
import * as path from 'path';
import * as vscode from 'vscode';
import {
    mvcWithComponentsRoot,
    pollUntil,
    waitForProjectReady,
} from './TestUtil';

let razorPath: string;

suite('Formatting', () => {
    before(async () => {
        await waitForProjectReady(mvcWithComponentsRoot);
    });

    beforeEach(async () => {
        razorPath = path.join(mvcWithComponentsRoot, 'Views', 'Shared', 'NavMenu.razor');
    });

    afterEach(async () => {
        await vscode.commands.executeCommand('workbench.action.revertAndCloseActiveEditor');
        await pollUntil(async () => {
            await vscode.commands.executeCommand('workbench.action.closeAllEditors');
            if (vscode.window.visibleTextEditors.length === 0) {
                return true;
            }

            return false;
        }, /* timeout */ 3000, /* pollInterval */ 500, true /* suppress timeout */);
    });

    test('Can format code block directives within .razor', async () => {
        const razorDoc = await vscode.workspace.openTextDocument(razorPath);
        const razorEditor = await vscode.window.showTextDocument(razorDoc);
        const options = {
            tabSize: 4,
            insertSpaces: true,
        };
        const input = `
@code {public class Foo{}}
`;
        const expected = `
@code {
    public class Foo { }
}
`;
        const fullRange = getFullRange(razorDoc);
        await razorEditor.edit(edit => edit.replace(fullRange, input));

        await new Promise(r => setTimeout(r, 5000));
        const edits = await vscode.commands.executeCommand<vscode.TextEdit[]>(
            'vscode.executeFormatDocumentProvider',
            razorDoc.uri,
            options) as vscode.TextEdit[];

        const workspaceEdit = new vscode.WorkspaceEdit();
        workspaceEdit.set(razorDoc.uri, edits);
        const result = await vscode.workspace.applyEdit(workspaceEdit);

        assert.equal(result, true);
        const text = razorDoc.getText();
        assert.equal(normalize(text), normalize(expected));
    });

    test('Can format multiple code block directives within .razor', async () => {
        const razorDoc = await vscode.workspace.openTextDocument(razorPath);
        const razorEditor = await vscode.window.showTextDocument(razorDoc);
        const options = {
            tabSize: 4,
            insertSpaces: true,
        };
        const input = `
    @code {
public class Foo
{
    void Method(){}
}}

Hello World

@code{      public class Baz {
                    public int Prop {get;set;}
    }}

The below block should not be formatted
@functions{
@* Foo *@
}
Same goes for the one below
@code{
<span></span>
}
`;
        const expected = `
@code {
    public class Foo
    {
        void Method() { }
    }
}

Hello World

@code{
    public class Baz
    {
        public int Prop { get; set; }
    }
}

The below block should not be formatted
@functions{
@* Foo *@
}
Same goes for the one below
@code{
<span></span>
}
`;
        const fullRange = getFullRange(razorDoc);
        await razorEditor.edit(edit => edit.replace(fullRange, input));

        await new Promise(r => setTimeout(r, 5000));
        const edits = await vscode.commands.executeCommand<vscode.TextEdit[]>(
            'vscode.executeFormatDocumentProvider',
            razorDoc.uri,
            options) as vscode.TextEdit[];

        const workspaceEdit = new vscode.WorkspaceEdit();
        workspaceEdit.set(razorDoc.uri, edits);
        const result = await vscode.workspace.applyEdit(workspaceEdit);

        assert.equal(result, true);
        const text = razorDoc.getText();
        assert.equal(normalize(text), normalize(expected));
    });

    function getFullRange(document: vscode.TextDocument) {
        const start = new vscode.Position(0, 0);
        const lastLine = document.lineAt(document.lineCount - 1);
        const end = new vscode.Position(lastLine.lineNumber, lastLine.rangeIncludingLineBreak.end.character);
        return new vscode.Range(start, end);
    }

    function normalize(text: string) {
        return text.split('\r\n').join('\n');
    }
});
