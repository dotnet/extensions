/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'microsoft.aspnetcore.razor.vscode/dist/vscodeAdapter';

export class TestUri implements vscode.Uri {
    constructor(
        public readonly path: string,
        public readonly query = path,
        public readonly scheme = 'unknown',
        public readonly authority = 'unknown',
        public readonly fragment = 'unknown',
        public readonly fsPath = path) {
    }

    public with(change: {
        scheme?: string;
        authority?: string;
        path?: string;
        query?: string;
        fragment?: string;
    }): vscode.Uri {
        throw new Error('Not Implemented.');
    }

    public toString(skipEncoding?: boolean): string {
        throw new Error('Not Implemented.');
    }

    public toJSON(): any {
        throw new Error('Not Implemented.');
    }
}
