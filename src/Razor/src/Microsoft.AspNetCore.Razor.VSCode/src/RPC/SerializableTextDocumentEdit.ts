/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import { SerializableTextEdit } from './SerializableTextEdit';

export interface SerializableTextDocumentEdit {
    kind: undefined;
    textDocument: {
        uri: string;
        version: number;
    };
    edits: Array<SerializableTextEdit>;
}
