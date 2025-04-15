import { getAccessToken, getService } from "azure-devops-extension-sdk";
import { IVssRestClientOptions } from "./Context";
import { CommonServiceIds, ILocationService } from "./CommonServices";

/**
 * A REST client factory is the method used to create and initialize an IVssRestClient.
 */
export type RestClientFactory<T> = {
    new (options: IVssRestClientOptions): T;
    RESOURCE_AREA_ID?: string;
}

export function getClient<T>(clientClass: RestClientFactory<T>, clientOptions?: IVssRestClientOptions) {

    const options = clientOptions || {};

    if (!options.rootPath) {
        options.rootPath = getService<ILocationService>(CommonServiceIds.LocationService).then(locationService => {
            if (clientClass.RESOURCE_AREA_ID) {
                return locationService.getResourceAreaLocation(clientClass.RESOURCE_AREA_ID);
            }
            else {
                return locationService.getServiceLocation();
            }
        });
    }

    if (!options.authTokenProvider) {
        options.authTokenProvider = {
            getAuthorizationHeader: (): Promise<string> => {
                return getAccessToken().then(token => token ? ("Bearer " + token) : "");
            }
        };
    }

    return new clientClass(options);
}