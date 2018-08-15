/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as path from 'path';

export const repoRoot = path.join(__dirname, '..', '..', '..');
export const basicRazorAppRoot = path.join(repoRoot, 'test', 'testapps', 'BasicRazorApp');

export async function pollUntil(fn: () => boolean, timeoutMs: number) {
    const pollInterval = 50;
    let timeWaited = 0;
    while (!fn()) {
        if (timeWaited >= timeoutMs) {
            throw new Error(`Timed out after ${timeoutMs}ms.`);
        }

        await new Promise(r => setTimeout(r, pollInterval));
        timeWaited += pollInterval;
    }
}
