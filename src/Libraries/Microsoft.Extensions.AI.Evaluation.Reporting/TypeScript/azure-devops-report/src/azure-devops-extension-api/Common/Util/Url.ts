/*
 * ---------------------------------------------------------
 * Copyright(C) Microsoft Corporation. All rights reserved.
 * ---------------------------------------------------------
 */

/**
 * Key constants used by route parsing.
 */
const enum KeyCodes {
    asterisk = 42,
    endCurlyBrace = 125,
    startCurlyBrace = 123
}

/**
* A single query parameter entry in a Uri
*/
export interface IQueryParameter {

    /**
    * Unencoded name of the query parameter
    */
    name: string;

    /**
    * Unencoded value of the query parameter
    */
    value: string | null;
}

/**
* Options for parsing a Uri string
*/
export interface IUriParseOptions {

    /**
    * If true, throw if the Uri is not absolute
    */
    absoluteUriRequired?: boolean;
}

/**
 * Type of individual entry values that can be used in Uri.addQueryParams call
 */
export type QueryParameterEntryValueType = string | boolean | number | Date | undefined;

/**
 * Type of values supported by Uri.addQueryParams call
 */
export type QueryParameterValueType =
    | QueryParameterEntryValueType
    | Array<QueryParameterEntryValueType>
    | { [key: string]: QueryParameterEntryValueType };

function prepareForComparison(value: string, upperCase: boolean): string {
    return value ? (upperCase ? value.toLocaleUpperCase() : value) : "";
}

function stringEquals(str1: string, str2: string, ignoreCase: boolean): boolean {
    if (str1 === str2) {
        return true;
    }
    return prepareForComparison(str1, ignoreCase).localeCompare(prepareForComparison(str2, ignoreCase)) === 0;
}

/**
* Class that represents a Uri and allows parsing/getting and setting of individual parts
*/
export class Uri {

    /**
    * The uri scheme such as http or https
    */
    public scheme: string = "";

    /**
     * If true, do not emit the "//" separator after the scheme:
     * Set to true for schemes like mailto (e.g. mailto:foo@bar)
     */
    public noSchemeSeparator: boolean = false;

    /**
    * The uri hostname (does not include port or scheme)
    */
    public host: string = "";

    /**
    * The port number of the uri as supplied in the url. 0 if left out in the url (e.g. the default port for the scheme).
    */
    public port: number = 0;

    /**
    * The relative path of the uri
    */
    public path: string = "";

    /**
    * The array of query parameters in the uri
    */
    public queryParameters: IQueryParameter[] = [];

    /**
    * The hash string of the uri
    */
    public hashString: string = "";

    /**
    * Create a new Uri.
    *
    * @param uri Optional uri string to populate values with
    * @param options Options for parsing the uri string
    */
   constructor(uri?: string, options?: IUriParseOptions) {
        if (uri) {
            this._setFromUriString(uri, options);
        }
    }

    private _setFromUriString(uriString: string, options?: IUriParseOptions) {
        let uri = uriString;

        // Parse out the hash string
        const hashSplit = this._singleSplit(uri, "#");
        if (hashSplit) {
            uri = hashSplit.part1;
            this.hashString = this._decodeUriComponent(hashSplit.part2);
        }
        else {
            this.hashString = "";
        }

        // Parse the query parameters
        const querySplit = this._singleSplit(uri, "?");
        if (querySplit) {
            uri = querySplit.part1;
            this.queryString = querySplit.part2;
        }
        else {
            this.queryParameters = [];
        }

        this.scheme = "";
        this.host = "";
        this.port = 0;
        this.path = "";

        // Parse out the scheme components of the uri
        this.noSchemeSeparator = false;
        const schemeSplit = this._singleSplit(uri, ":");
        if (schemeSplit) {
            this.scheme = schemeSplit.part1;
            uri = schemeSplit.part2;

            if (uri.substr(0, 2) === "//") {
                uri = uri.substr(2);

                // Parse out the path part of the uri
                const pathSplit = this._singleSplit(uri, "/");
                if (pathSplit) {
                    uri = pathSplit.part1;
                    this.path = pathSplit.part2;
                }
                else {
                    this.path = "";
                }

                // Parse out the port number
                const portSplit = this._singleSplit(uri, ":");
                if (portSplit) {
                    this.host = portSplit.part1;
                    this.port = parseInt(portSplit.part2);

                    if (isNaN(this.port)) {
                        // Segment after : was not a port, consider it part of the path
                        this.host += ":";
                        this.path = portSplit.part2 + "/" + this.path;
                    }
                }
                else {
                    this.host = uri;
                }
            }
            else {
                // No host for schemes like mailto: just use path
                this.noSchemeSeparator = true;
                this.path = uri;
            }
        }
        else {
            // Relative Url was given
            this.path = uri;
        }

        if (options && options.absoluteUriRequired && !this.scheme) {
            throw new Error(`The uri string "${uriString}" does not represent a valid absolute uri.`);
        }
    }

    private _singleSplit(value: string, separator: string): { part1: string, part2: string } | undefined {
        const matchIndex = value.indexOf(separator);
        if (matchIndex >= 0) {
            return {
                part1: value.substr(0, matchIndex),
                part2: value.substr(matchIndex + 1)
            };
        }
        else {
            return undefined;
        }
    }

    private _decodeUriComponent(value: string): string {
        if (value) {
            // Replace "+" character with %20.
            value = value.replace(/\+/g, "%20");
            value = decodeURIComponent(value);
        }
        return value;
    }

    /**
    * Get the absolute uri string for this Uri
    */
    public get absoluteUri(): string {
        let uri = "";

        if (this.scheme) {
            uri = encodeURI(decodeURI(this.scheme)) + ":";
            if (!this.noSchemeSeparator) {
                uri += "//";
            }
        }

        if (this.host) {
            uri += encodeURI(decodeURI(this.host));

            if (this.port) {
                uri += ":" + this.port;
            }

            if (!this.noSchemeSeparator || this.path) {
                uri += "/";
            }
        }

        if (this.path) {
            let encodedPath: string;
            if (this.noSchemeSeparator) {
                // Only do simple encoding for schemes like mailto: or blob: where
                // we can't determine host versus path
                encodedPath = encodeURI(decodeURI(this.path));
            }
            else {
                const parts = this.path.split('/');
                encodedPath = parts.map(p => encodeURIComponent(decodeURIComponent(p))).join("/");
            }
            
            if (this.host) {
                uri = combineUrlPaths(uri, encodedPath);
            }
            else {
                uri = uri + encodedPath;
            }
        }

        const queryString = this.queryString;
        if (queryString) {
            uri += "?" + queryString;
        }

        if (this.hashString) {
            const params = this._splitStringIntoParams(this.hashString);
            const encodedString = this._getParamsAsString(params);
            uri += "#" + encodedString;
        }

        return uri;
    }

    /**
    * Set the absolute uri string for this Uri. Replaces all existing values
    */
    public set absoluteUri(uri: string) {
        this._setFromUriString(uri || "");
    }

    /**
     * Gets the effective port number, returning the default port number if omitted for the given scheme.
     */
    public getEffectivePort(): number {
        if (this.port) {
            return this.port;
        }
        else {
            if (stringEquals(this.scheme, "http", true)) {
                return 80;
            }
            else if (stringEquals(this.scheme, "https", true)) {
                return 443;
            }
            else {
                return 0;
            }
        }
    }

    /**
     * Builds an encoded key/value pair string 
     * like query string or hash strings
     */
    private _getParamsAsString(params: IQueryParameter[]): string {
        if (params && params.length) {
            return params.map((param) => {
                if (param.value !== null) {
                    return encodeURIComponent(param.name) + "=" + encodeURIComponent(param.value);
                }
                else {
                    return encodeURIComponent(param.name);
                }
            }).join("&");
        }
        else {
            return "";
        }
    }

    /**
    * Get the query string for this Uri.
    */
    public get queryString(): string {
        return this._getParamsAsString(this.queryParameters);
    }

    /**
    * Set the query string for this Uri. Replaces existing value
    */
    public set queryString(queryString: string) {
        this.queryParameters = this._splitStringIntoParams(queryString);
    }

    /**
     * Coverts a key/value pair string into parameters array
     * @param paramString String such as a=b&c=d
     */
    private _splitStringIntoParams(paramString: string): IQueryParameter[] {
        const params: IQueryParameter[] = [];
        paramString.split('&').forEach((pair) => {
            if (pair) {
                const valueSplit = this._singleSplit(pair, "=");
                if (valueSplit) {
                    params.push({
                        name: this._decodeUriComponent(valueSplit.part1),
                        value: this._decodeUriComponent(valueSplit.part2)
                    });
                }
                else {
                    params.push({
                        name: this._decodeUriComponent(pair),
                        value: null
                    });
                }
            }
        });

        return params;
    }

    /**
    * Get the value of the query parameter with the given key
    *
    * @param name Query parameter name
    */
    public getQueryParam(name: string): string | null | undefined {
        let value: string | undefined | null;
        if (this.queryParameters) {
            const matchingPairs = this.queryParameters.filter(p => stringEquals(p.name, name, true));
            if (matchingPairs.length > 0) {
                value = matchingPairs[0].value;
            }
        }
        return value;
    }

    /**
     * Adds a query string parameter to the current uri
     *
     * @param name The Query parameter name
     * @param value The Query parameter value
     * @param replaceExisting If true, replace all existing parameters with the same name
     */
    public addQueryParam(name: string, value: string | null, replaceExisting?: boolean): void {
        if (replaceExisting) {
            this.removeQueryParam(name);
        }
        if (!this.queryParameters) {
            this.queryParameters = [];
        }
        this.queryParameters.push({ name: name, value: value });
    }

    /**
     * Adds query string parameters to the current uri
     *
     * @param parameters Query parameters to add
     * @param replaceExisting If true, replace all existing parameters with the same name
     * @param keyPrefix If specified, a value to prepend to all query parameter keys
     */
    public addQueryParams(parameters: { [key: string]: QueryParameterValueType }, replaceExisting?: boolean, keyPrefix?: string): void {
        for (const key in parameters) {
            const value = parameters[key];
            if (value !== null && value !== undefined) {
                const keyWithPrefix = (keyPrefix || "") + key;
                if (value instanceof Date) {
                    this.addQueryParam(keyWithPrefix, value.toJSON(), replaceExisting);
                } else if (Array.isArray(value)) {
                    value.forEach(v => this.addQueryParam(keyWithPrefix, "" + v, replaceExisting));
                } else if (typeof value === "object") {
                    this.addQueryParams(value, replaceExisting, keyWithPrefix + ".");
                } else {
                    this.addQueryParam(keyWithPrefix, "" + value, replaceExisting);
                }
            }
        }
    }

    /**
     * Removes a query string parameter
     * 
     * @param name The Query parameter name
     */
    public removeQueryParam(name: string): void {
        if (this.queryParameters) {
            this.queryParameters = this.queryParameters.filter(p => !stringEquals(p.name, name, true));
        }
    }
}

/**
 * Take url segments and join them with a single slash character. Takes care of path segements that start and/or end
 * with a slash to ensure that the resulting URL does not have double-slashes
 * 
 * @param paths Path segments to concatenate
 */
export function combineUrlPaths(...paths: string[]): string {

    const segmentsToJoin: string[] = [];

    // Trim leading and trailing slash in each segment
    for (let i = 0, last = paths.length - 1; i <= last; i++) {
        let path = paths[i];
        if (path) {
            if (path === "/" && (i === 0 || i === last)) {
                // For a "/" segment at the beginning or end of the list, insert an empty entry to force
                // a leading or trailing slash in the resulting URL
                segmentsToJoin.push("");
            }
            else {
                if (i > 0 && path[0] === "/") {
                    // Trim leading slash in any segment except the first one
                    path = path.substr(1);
                }
                if (i < last && path[path.length - 1] === "/") {
                    // Trim trailing slash in any segment except the last one
                    path = path.substr(0, path.length - 1);
                }
                if (path) {
                    segmentsToJoin.push(path);
                }
            }
        }
    }

    if (segmentsToJoin.length === 1 && segmentsToJoin[0] === "") {
        return "/";
    }

    // Join segments by slash
    return segmentsToJoin.join("/");
}

/**
 * Represents a route parsed by parseRoute
 */
export interface IParsedRoute {

    /**
     * Array of the segements in the route
     */
    segments: IParsedRouteSegment[];
}

/**
 * And individual segment of the route (fixed text or a parameter)
 */
export interface IParsedRouteSegment {

    /**
     * If present, the fixed text for this segement. Either text or paramName will be defined for a segment, never both.
     */
    text?: string;

    /**
     * If present, the name of the route value parameter to substitute for this segment. Either text or paramName will be defined for a segment, never both.
     */
    paramName?: string;

    /**
     * For parameters, whether or not this parameter is a wildcard (*) parameter, which means it allows multiple path segments (i.e. don't escape "/")
     */
    isWildCardParam?: boolean;

    /**
     * Whether the parameter is required in order for the URL to be valid.
     */
    isRequiredParam?: boolean;
}

/**
 * Parse a route template into a structure that can be used to quickly do route replacements
 * 
 * @param routeTemplate MVC route template string (like "/foo/{id}/{*params}")
 */
export function parseRouteTemplate(routeTemplate: string): IParsedRoute {

    const parsedRoute: IParsedRoute = {
        segments: []
    };

    let paramStartIndex = -1;
    let segmentStartIndex = -1;
    let segmentPrefix = "";

    for (let charIndex = 0, routeTemplateLen = routeTemplate.length; charIndex < routeTemplateLen; charIndex++) {
        const c = routeTemplate.charCodeAt(charIndex);

        if (paramStartIndex >= 0) {
            if (c === KeyCodes.endCurlyBrace) {

                let paramName = routeTemplate.substring(paramStartIndex, charIndex);
                let isWildCardParam = false;

                if (paramName.charCodeAt(0) === KeyCodes.asterisk) {
                    paramName = paramName.substr(1);
                    isWildCardParam = true;
                }

                parsedRoute.segments.push({
                    paramName,
                    isWildCardParam
                });

                paramStartIndex = -1;
            }
        }
        else {
            if (c === KeyCodes.startCurlyBrace && routeTemplate.charCodeAt(charIndex + 1) !== KeyCodes.startCurlyBrace) {
                // Start of a parameter
                if (segmentPrefix || segmentStartIndex >= 0) {

                    // Store the previous segment
                    let segmentText = segmentPrefix;
                    if (segmentStartIndex >= 0) {
                        segmentText += routeTemplate.substring(segmentStartIndex, charIndex);
                    }

                    if (segmentText) {
                        parsedRoute.segments.push({
                            text: segmentText
                        });
                    }

                    // Reset the segment tracking info
                    segmentStartIndex = -1;
                    segmentPrefix = "";
                }
                paramStartIndex = charIndex + 1;
            }
            else {

                // Handle double {{ or double }} as an escape sequence. This is rare. For simplicity we will 
                if ((c === KeyCodes.startCurlyBrace && routeTemplate.charCodeAt(charIndex + 1) === KeyCodes.startCurlyBrace) ||
                    (c === KeyCodes.endCurlyBrace && routeTemplate.charCodeAt(charIndex + 1) === KeyCodes.endCurlyBrace)) {

                    segmentPrefix = segmentPrefix + routeTemplate.substring(segmentStartIndex >= 0 ? segmentStartIndex : charIndex, charIndex + 1);
                    segmentStartIndex = -1;
                    charIndex++;
                }

                if (segmentStartIndex < 0) {
                    segmentStartIndex = charIndex;
                }
            }
        }
    }

    // Store any pending segment
    if (segmentStartIndex >= 0 || paramStartIndex >= 0) {
        const segmentText = segmentPrefix + routeTemplate.substring(segmentStartIndex >= 0 ? segmentStartIndex : paramStartIndex);
        if (segmentText) {
            parsedRoute.segments.push({
                text: segmentText
            });
        }
    }

    // Mark any param as required if it has a text segment (other than just "/") after it
    let required = false;
    for (let i = parsedRoute.segments.length - 1; i >= 0; i--) {
        const segment = parsedRoute.segments[i];
        if (segment.text && segment.text !== "/") {
            required = true;
        }
        else if (required && segment.paramName) {
            segment.isRequiredParam = true;
        }
    }

    return parsedRoute;
}

/**
 * Take a set of routes and route values and form a url using the best match. The best match
 * is the route with the highest number of replacements (of the given routeValues dictionary).
 * In the event of a tie (same number of replacements) the route that came first wins.
 * 
 * @param routeCollection Array of parsed routes
 * @param routeValues Replacement values
 */
export function routeUrl(routeCollection: IParsedRoute[], routeValues: { [name: string]: string }): string {

    const bestMatch = getBestRouteMatch(routeCollection, routeValues);
    if (!bestMatch) {
        return "";
    }

    const uri = new Uri(bestMatch.url);
    for (const routeValueKey in routeValues) {
        if (!bestMatch.matchedParameters[routeValueKey]) {
            uri.addQueryParam(routeValueKey, routeValues[routeValueKey]);
        }
    }

    return uri.absoluteUri;
}

/**
 * Take a set of routes and find the best match. The best match is the route with the highest number of replacements
 * (of the given routeValues dictionary). In the event of a tie (same number of replacements) the route that came first wins.
 * 
 * @param routeCollection Array of parsed routes
 * @param routeValues Replacement values
 */
export function getBestRouteMatch(routeCollection: IParsedRoute[], routeValues: { [name: string]: string }): IRouteMatchResult | undefined {

    let bestMatch: IRouteMatchResult | undefined;
    const totalRouteValues = Object.keys(routeValues).length;

    for (const route of routeCollection) {
        const match = replaceParsedRouteValues(route, routeValues, false);
        if (match && (!bestMatch || match.matchedParametersCount > bestMatch.matchedParametersCount)) {
            bestMatch = match;

            if (match.matchedParametersCount === totalRouteValues) {
                // This route matched all route values. Return its url directly (no need to even add query params)
                return bestMatch;
            }
        }
    }

    return bestMatch;
}

/**
 * Result of a call to replace route values for a parsed route
 */
export interface IRouteMatchResult {

    /**
     * Resulting URL from the template replacement. Does NOT include any query parameters that would be added from extra route values. 
     */
    url: string;

    /**
     * Dictionary of the route value keys that were used as replacements
     */
    matchedParameters: { [key: string]: boolean };

    /**
     * The number of parameter replacements made
     */
    matchedParametersCount: number;
}

/**
 * Replace route values for a specific parsed route
 * 
 * @param parsedRoute The route to evaluate
 * @param routeValues Dictionary of route replacement parameters
 * @param continueOnUnmatchedSegements If true, continue with replacements even after a miss. By default (false), stop replacements once a parameter is not present.
 */
export function replaceParsedRouteValues(parsedRoute: IParsedRoute, routeValues: { [name: string]: string | number | undefined }, continueOnUnmatchedSegements?: boolean): IRouteMatchResult | undefined {

    const urlParts: string[] = [];
    const matchedParameters: { [key: string]: boolean } = {};
    let matchedParametersCount = 0;

    for (let segmentIndex = 0, l = parsedRoute.segments.length; segmentIndex < l; segmentIndex++) {
        const segment = parsedRoute.segments[segmentIndex];

        if (segment.text) {
            let segmentText = segment.text;
            if (continueOnUnmatchedSegements) {
                // Make sure we don't have consecutive slash (/) characters in the case of missing segments
                if (segmentIndex > 0 && segmentText.charAt(0) === "/") {
                    if (urlParts.length === 0) {
                        // First text segment after one or more missing parameter segments. Remove the leading slash.
                        segmentText = segmentText.substr(1);
                    }
                }
            }

            if (segmentText) {
                urlParts.push(segmentText);
            }
        }
        else {
            const value = routeValues[segment.paramName!];
            if (!value && value !== 0) {
                // The route value was not supplied
                if (!continueOnUnmatchedSegements) {
                    if (segment.isRequiredParam) {
                        // This is a required parameter. Return undefined since this route was not a match.
                        return undefined;
                    }
                    else {
                        // This is an omitted optional parameter. Return what we've computed so far.
                        break;
                    }
                }
                else if (urlParts.length) {
                    // Unmatched segment being omitted. Remove any previously trailing slash
                    const lastSegment = urlParts[urlParts.length - 1];
                    if (lastSegment[lastSegment.length - 1] === "/") {
                        urlParts[urlParts.length - 1] = lastSegment.substr(0, lastSegment.length - 1);
                    }
                }
            }
            else {
                urlParts.push(segment.isWildCardParam ? encodeURI("" + value) : encodeURIComponent("" + value));
                matchedParameters[segment.paramName!] = true;
                matchedParametersCount++;
            }
        }
    }

    return {
        url: urlParts.join(""),
        matchedParameters,
        matchedParametersCount
    };
}

/**
 * Take an MVC route template (like "/foo/{id}/{*params}") and replace the templated parts with values from the given dictionary
 * 
 * @param routeTemplate MVC route template (like "/foo/{id}/{*params}")
 * @param routeValues Route value replacements
 */
export function replaceRouteValues(routeTemplate: string, routeValues: { [name: string]: string | number | undefined }): string {
    const parsedRoute = parseRouteTemplate(routeTemplate);
    return replaceParsedRouteValues(parsedRoute, routeValues, true)!.url;
}