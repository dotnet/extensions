/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';

// We'd typically just use vscode.Position here; however, that type doesn't serialize properly over the wire.
export interface SerializablePosition {
    readonly line: number;
    readonly character: number;
}

export function convertPositionToSerializable(position: vscode.Position): SerializablePosition {
    return {
        line: position.line,
        character: position.character,
    };
}

export function convertPositionFromSerializable(position: SerializablePosition): vscode.Position {
    return new vscode.Position(position.line, position.character);
}
