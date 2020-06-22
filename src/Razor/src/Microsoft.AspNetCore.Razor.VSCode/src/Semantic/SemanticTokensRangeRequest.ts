/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { TextDocumentIdentifier } from 'vscode-languageclient';
import { convertRangeToSerializable, SerializableRange } from '../RPC/SerializableRange';

export class SemanticTokensRangeRequest {
    public readonly range: SerializableRange;
    public readonly textDocument: TextDocumentIdentifier;

    constructor(
        razorDocumentUri: vscode.Uri,
        range: vscode.Range,
    ) {
        this.textDocument = TextDocumentIdentifier.create(razorDocumentUri.toString());
        this.range = convertRangeToSerializable(range);
    }
}
