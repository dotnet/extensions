/* --------------------------------------------------------------------------------------------
* Copyright (c) Microsoft Corporation. All rights reserved.
* Licensed under the MIT License. See License.txt in the project root for license information.
* ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { convertRangeFromSerializable, convertRangeToSerializable, SerializableRange } from './SerializableRange';

// We'd typically just use vscode.TextEdit here; however, that type doesn't serialize properly over the wire.
export interface SerializableTextEdit {
    readonly range: SerializableRange;
    readonly newText: string;
}

export function convertTextEditToSerializable(textEdit: vscode.TextEdit): SerializableTextEdit {
    return {
        range: convertRangeToSerializable(textEdit.range),
        newText: textEdit.newText,
    };
}

export function convertTextEditFromSerializable(textEdit: SerializableTextEdit): vscode.TextEdit {
    return new vscode.TextEdit(
        convertRangeFromSerializable(textEdit.range),
        textEdit.newText);
}
