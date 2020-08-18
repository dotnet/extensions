/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import { ITokenizedContent } from './ITokenizedContent';

export function createSnapshot(tokenizedContent: ITokenizedContent): string {
    const snapshotLines: string[] = [];
    for (let i = 0; i < tokenizedContent.tokenizedLines.length; i++) {
        const line = tokenizedContent.lines[i];
        const tokenizedLine = tokenizedContent.tokenizedLines[i];

        snapshotLines.push(`Line: ${line}`);
        for (const token of tokenizedLine.tokens) {
            snapshotLines.push(` - token from ${token.startIndex} to ${token.endIndex} ` +
                `(${line.substring(token.startIndex, token.endIndex)}) ` +
                `with scopes ${token.scopes.join(', ')}`);
        }
        snapshotLines.push('');
    }

    const snapshot = snapshotLines.join('\n');
    return snapshot;
}
