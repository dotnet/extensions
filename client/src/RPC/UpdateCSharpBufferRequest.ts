/* --------------------------------------------------------------------------------------------
* Copyright (c) Microsoft Corporation. All rights reserved.
* Licensed under the MIT License. See License.txt in the project root for license information.
* ------------------------------------------------------------------------------------------ */

import { ServerTextChange } from './ServerTextChange';

export class UpdateCSharpBufferRequest {
    constructor(
        public readonly hostDocumentVersion: number,
        public readonly hostDocumentFilePath: string,
        public readonly changes: ServerTextChange[]) {
    }
}
