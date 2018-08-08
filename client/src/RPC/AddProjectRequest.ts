/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

export class AddProjectRequest {
    constructor(public readonly filePath: string, public readonly configurationName?: string) {
    }
}
