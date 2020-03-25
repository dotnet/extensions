/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

 export class SemanticTokensEdit {
    public readonly start: number;
    public readonly deleteCount: number;
    public readonly data?: Uint32Array;
    constructor(start: number, deleteCount: number, data?: Uint32Array) {
        this.start = start;
        this.deleteCount = deleteCount;
        this.data = data;
    }
}
