// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/**
 * Contribution ids of core DevOps services which can be obtained from DevOps.getService
 */
export const enum CommonServiceIds {

    /**
     * Service for getting URLs/locations from the host context
     */
    LocationService = "ms.vss-features.location-service",

}

/**
 * Host level for a VSS service
 */
export const enum TeamFoundationHostType {
    /**
     * The Deployment host
     */
    Deployment = 1,

    /**
     * The Enterprise host
     */
    Enterprise = 2,

    /**
     * The organization/project collection host
     */
    Organization = 4
}

/**
 * Service for external content to get locations
 */
export interface ILocationService {

    /**
     * Gets the URL for the given REST resource area 
     * 
     * @param resourceAreaId - Id of the resource area
     */
    getResourceAreaLocation(resourceAreaId: string): Promise<string>;

    /**
     * Gets the location of a remote service at a given host type.
     *
     * @param serviceInstanceType - The GUID of the service instance type to lookup
     * @param hostType - The host type to lookup (defaults to the host type of the current page data)
     */
    getServiceLocation(serviceInstanceType?: string, hostType?: TeamFoundationHostType): Promise<string>;

    /**
     * Attemps to create a url for the specified route template and paramaters.  The url will include host path.
     * For example, if the page url is https://dev.azure.com/foo and you try to create admin settings url for project "bar",
     * the output will be /foo/bar/_admin.
     * 
     * This will asynchronously fetch a route contribution if it has not been included in page data.
     * 
     * @param routeId - Id of the route contribution
     * @param routeValues - Route value replacements
     * @param hostPath - Optional host path to use rather than the default host path for the page.
     */
    routeUrl(routeId: string, routeValues?: { [key: string]: string }, hostPath?: string): Promise<string>;
}
