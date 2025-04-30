/* eslint-disable @typescript-eslint/no-explicit-any */
/*
 * ---------------------------------------------------------
 * Copyright(C) Microsoft Corporation. All rights reserved.
 * ---------------------------------------------------------
 */

const msAjaxDateRegEx = new RegExp('(^|[^\\\\])\\"\\\\/Date\\((-?[0-9]+)(?:[a-zA-Z]|(?:\\+|-)[0-9]{4})?\\)\\\\/\\"', 'g');

/**
 * Handle the raw text of a JSON response which may contain MSJSON style dates and
 * deserialize (JSON.parse) the data in a way that restores Date objects rather than
 * strings.
 * 
 * MSJSON style dates are in the form:
 * 
 *     "lastModified": "\/Date(1496158224000)\/"
 * 
 * This format unnecessarily (but intentionally) escapes the "/" character. So the parsed
 * JSON will have no trace of the escape character (\).
 * 
 * @param text The raw JSON text
 */
export function deserializeVssJsonObject<T>(text: string): T | null {

    function replaceMsJsonDates(object: any, parentObject: any, parentObjectKey: string) {

        if (parentObject && typeof object.__msjson_date__ === "number") {
            parentObject[parentObjectKey] = new Date(object.__msjson_date__);
            return;
        }

        for (const key in object) {
            const value = object[key];
            if (value !== null && typeof value === "object") {
                replaceMsJsonDates(object[key], object, key);
            }
        }
    }

    let deserializedData: T | null = null;

    if (text) {

        // Replace MSJSON dates with an object that we can easily identify after JSON.parse.
        // This replaces the string value (like "\/Date(1496158224000)\/") with a JSON object that 
        // has an "__msjson_date__" key.
        const replacedText = text.replace(msAjaxDateRegEx, "$1{\"__msjson_date__\":$2 }");

        // Do the actual JSON deserialization
        deserializedData = JSON.parse(replacedText);

        // Go through the parsed object and create actual Date objects for our replacements made above
        if (replacedText !== text) {
            replaceMsJsonDates(deserializedData, null, "");
        }
    }

    return deserializedData;
}