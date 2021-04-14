/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import { RazorLanguage } from 'microsoft.aspnetcore.razor.vscode/dist/RazorLanguage';
import * as vscode from 'microsoft.aspnetcore.razor.vscode/dist/vscodeAdapter';
import { EndOfLine } from 'microsoft.aspnetcore.razor.vscode/dist/vscodeAdapter';
import * as os from 'os';

export class TestTextDocument implements vscode.TextDocument {

    public readonly isUntitled = false;

    public readonly version = 0;

    public readonly isDirty: boolean = false;

    public readonly lineCount: number;

    constructor(
        private readonly content: string,
        public readonly uri: vscode.Uri,
        public readonly languageId: string = RazorLanguage.id) {

        this.lineCount = content.split(os.EOL).length;
    }

    public get fileName(): string {
        throw new Error('Not implemented');
    }

    public get isClosed(): boolean {
        throw new Error('Not implemented');
    }

    public get eol(): EndOfLine {
        if (os.EOL.startsWith('\r')) {
            return EndOfLine.CRLF;
        }

        return EndOfLine.LF;
    }

    public save(): vscode.Thenable<boolean> {
        return new Promise<boolean>((resolve) => resolve(true));
    }

    public lineAt(line: any): vscode.TextLine {
        throw new Error('Not implemented');
    }

    public offsetAt(position: vscode.Position): number {
        throw new Error('Not implemented');
    }

    public positionAt(offset: number): vscode.Position {
        throw new Error('Not implemented');
    }

    public getText(range?: vscode.Range): string {
        if (range) {
            throw new Error('getText is not implemented with range');
        }

        return this.content;
    }

    public getWordRangeAtPosition(position: vscode.Position, regex?: RegExp): vscode.Range | undefined {
        throw new Error('Not implemented');
    }

    public validateRange(range: vscode.Range): vscode.Range {
        throw new Error('Not implemented');
    }

    public validatePosition(position: vscode.Position): vscode.Position {
        throw new Error('Not implemented');
    }
}
