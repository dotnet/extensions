/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

export class SemanticTokensResponse {
    constructor(
        public readonly data: Array<number>,
        public readonly resultId?: string) {
    }
}
