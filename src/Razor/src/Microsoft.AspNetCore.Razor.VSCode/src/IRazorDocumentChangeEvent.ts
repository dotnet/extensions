/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import { IRazorDocument } from './IRazorDocument';
import { RazorDocumentChangeKind } from './RazorDocumentChangeKind';

export interface IRazorDocumentChangeEvent {
    readonly document: IRazorDocument;
    readonly kind: RazorDocumentChangeKind;
}
