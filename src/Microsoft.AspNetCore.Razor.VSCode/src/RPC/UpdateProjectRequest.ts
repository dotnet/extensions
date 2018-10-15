/* --------------------------------------------------------------------------------------------
* Copyright (c) Microsoft Corporation. All rights reserved.
* Licensed under the MIT License. See License.txt in the project root for license information.
* ------------------------------------------------------------------------------------------ */

export interface UpdateProjectRequest {
    readonly projectFilePath: string;
    readonly targetFramework: string;
    readonly tagHelpers: any[];
    readonly configuration: any;
}
