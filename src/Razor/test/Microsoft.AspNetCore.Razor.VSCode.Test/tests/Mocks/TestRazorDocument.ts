/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import { IProjectedDocument } from 'microsoft.aspnetcore.razor.vscode/dist/IProjectedDocument';
import { IRazorDocument } from 'microsoft.aspnetcore.razor.vscode/dist/IRazorDocument';
import * as vscode from 'microsoft.aspnetcore.razor.vscode/dist/vscodeAdapter';
import { TestProjectedDocument } from './TestProjectedDocument';
import { TestUri } from './TestUri';

export class TestRazorDocument implements IRazorDocument {
    public csharpDocument: IProjectedDocument;
    public htmlDocument: IProjectedDocument;

    constructor(
        public readonly uri: vscode.Uri,
        public readonly path: string = uri.path) {
        this.csharpDocument = new TestProjectedDocument('C#', new TestUri(`${this.path}.cs`));
        this.htmlDocument = new TestProjectedDocument('Html', new TestUri(`${this.path}.cs`));
    }
}
