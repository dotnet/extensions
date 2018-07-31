/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

export class AddProjectRequest {
    constructor (filePath: string, configurationName?: string) {
        this.filePath = filePath;
        this.configurationName = configurationName;
    }

    public readonly filePath:  string;
    public readonly configurationName:  string | undefined;
}