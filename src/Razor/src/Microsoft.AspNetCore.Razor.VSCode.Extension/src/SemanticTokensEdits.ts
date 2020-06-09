/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import { SemanticTokensEdit } from './SemanticTokensEdit';

export class SemanticTokensEdits {
    public readonly resultId?: string;
    public readonly edits: SemanticTokensEdit[];
    constructor(edits: SemanticTokensEdit[], resultId?: string) {
        this.resultId = resultId;
        this.edits = edits;
    }
}
