/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import { IRazorProject } from './IRazorProject';
import { RazorProjectChangeKind } from './RazorProjectChangeKind';

export interface IRazorProjectChangeEvent {
    readonly project: IRazorProject;
    readonly kind: RazorProjectChangeKind;
}
