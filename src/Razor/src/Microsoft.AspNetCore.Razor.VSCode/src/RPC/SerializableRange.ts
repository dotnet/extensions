/* --------------------------------------------------------------------------------------------
* Copyright (c) Microsoft Corporation. All rights reserved.
* Licensed under the MIT License. See License.txt in the project root for license information.
* ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { convertPositionFromSerializable, convertPositionToSerializable, SerializablePosition } from './SerializablePosition';

// We'd typically just use vscode.Range here; however, that type doesn't serialize properly over the wire.
export interface SerializableRange {
    readonly start: SerializablePosition;
    readonly end: SerializablePosition;
}

export function convertRangeToSerializable(range: vscode.Range): SerializableRange {
    return {
        start: convertPositionToSerializable(range.start),
        end: convertPositionToSerializable(range.end),
    };
}

export function convertRangeFromSerializable(range: SerializableRange): vscode.Range {
    return new vscode.Range(
        convertPositionFromSerializable(range.start),
        convertPositionFromSerializable(range.end));
}
