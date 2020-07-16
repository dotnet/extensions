/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

 export interface SerializableRenameDocument {
    kind: 'rename';
    oldUri: string;
    newUri: string;
    options: {
        overwrite: boolean;
        ignoreIfExists: boolean;
    };
}
