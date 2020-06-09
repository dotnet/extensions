/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { LanguageKind } from './LanguageKind';
import { SerializableRange } from './SerializableRange';

export class RazorMapToDocumentRangesRequest {
    public readonly razorDocumentUri: string;

    constructor(
        public readonly kind: LanguageKind,
        public readonly projectedRanges: SerializableRange[],
        razorDocumentUri: vscode.Uri) {
        this.razorDocumentUri = razorDocumentUri.toString();
    }
}
