/*
 * ---------------------------------------------------------
 * Copyright(C) Microsoft Corporation. All rights reserved.
 * ---------------------------------------------------------
 */

import { IVssRestClientOptions } from "../Common/Context";
import { RestClientBase } from "../Common/RestClientBase";

import * as Build from "./Build";

export class BuildRestClient extends RestClientBase {
    constructor(options: IVssRestClientOptions) {
        super(options);
    }

    public static readonly RESOURCE_AREA_ID = "965220d5-5bb9-42cf-8d67-9b146df2a5a4";
 
    /**
     * Gets the list of attachments of a specific type that are associated with a build.
     * 
     * @param project - Project ID or project name
     * @param buildId - The ID of the build.
     * @param type - The type of attachment.
     */
    public async getAttachments(
        project: string,
        buildId: number,
        type: string
        ): Promise<Build.Attachment[]> {

        return this.beginRequest<Build.Attachment[]>({
            apiVersion: "7.2-preview.2",
            routeTemplate: "{project}/_apis/build/builds/{buildId}/attachments/{type}",
            routeValues: {
                project: project,
                buildId: buildId,
                type: type
            }
        });
    }

}
