/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/
/* tslint:disable */

export type ProviderResult<T> = T | undefined | null | Thenable<T | undefined | null>;

/**
 * A text document content provider allows to add readonly documents
 * to the editor, such as source from a dll or generated html from md.
 *
 * Content providers are [registered](#workspace.registerTextDocumentContentProvider)
 * for a [uri-scheme](#Uri.scheme). When a uri with that scheme is to
 * be [loaded](#workspace.openTextDocument) the content provider is
 * asked.
 */
export interface TextDocumentContentProvider {

    /**
     * An event to signal a resource has changed.
     */
    onDidChange?: Event<Uri>;

    /**
     * Provide textual content for a given uri.
     *
     * The editor will use the returned string-content to create a readonly
     * [document](#TextDocument). Resources allocated should be released when
     * the corresponding document has been [closed](#workspace.onDidCloseTextDocument).
     *
     * **Note**: The contents of the created [document](#TextDocument) might not be
     * identical to the provided text due to end-of-line-sequence normalization.
     *
     * @param uri An uri which scheme matches the scheme this provider was [registered](#workspace.registerTextDocumentContentProvider) for.
     * @param token A cancellation token.
     * @return A string or a thenable that resolves to such.
     */
    provideTextDocumentContent(uri: Uri, token: CancellationToken): ProviderResult<string>;
}

/**
 * Represents a typed event.
 *
 * A function that represents an event to which you subscribe by calling it with
 * a listener function as argument.
 *
 * @sample `item.onDidChange(function(event) { console.log("Event happened: " + event); });`
 */
export interface Event<T> {

    /**
     * A function that represents an event to which you subscribe by calling it with
     * a listener function as argument.
     *
     * @param listener The listener function will be called when the event happens.
     * @param thisArgs The `this`-argument which will be used when calling the event listener.
     * @param disposables An array to which a [disposable](#Disposable) will be added.
     * @return A disposable which unsubscribes the event listener.
     */
    (listener: (e: T) => any, thisArgs?: any, disposables?: Disposable[]): Disposable;
}

/**
 * An event emitter can be used to create and manage an [event](#Event) for others
 * to subscribe to. One emitter always owns one event.
 *
 * Use this class if you want to provide event from within your extension, for instance
 * inside a [TextDocumentContentProvider](#TextDocumentContentProvider) or when providing
 * API to other extensions.
 */
export declare class EventEmitter<T> {
    /**
     * The event listeners can subscribe to.
     */
    event: Event<T>;

    /**
     * Notify all subscribers of the [event](EventEmitter#event). Failure
     * of one or more listener will not fail this function call.
     *
     * @param data The event object.
     */
    fire(data: T): void;

    /**
     * Dispose this object and free resources.
     */
    dispose(): void;
}

export interface OutputChannel {

    /**
     * The human-readable name of this output channel.
     */
    readonly name: string;

    /**
     * Append the given value to the channel.
     *
     * @param value A string, falsy values will not be printed.
     */
    append(value: string): void;

    /**
     * Append the given value and a line feed character
     * to the channel.
     *
     * @param value A string, falsy values will be printed.
     */
    appendLine(value: string): void;

    /**
     * Removes all output from the channel.
     */
    clear(): void;

    /**
     * Reveal this channel in the UI.
     *
     * @param preserveFocus When `true` the channel will not take focus.
     */
    show(preserveFocus?: boolean): void;

    /**
     * Hide this channel from the UI.
     */
    hide(): void;

    /**
     * Dispose and free associated resources.
     */
    dispose(): void;
}

export enum ViewColumn {
    /**
     * A *symbolic* editor column representing the currently
     * active column. This value can be used when opening editors, but the
     * *resolved* [viewColumn](#TextEditor.viewColumn)-value of editors will always
     * be `One`, `Two`, `Three`, or `undefined` but never `Active`.
     */
    Active = -1,
    /**
     * The left most editor column.
     */
    One = 1,
    /**
     * The center editor column.
     */
    Two = 2,
    /**
     * The right most editor column.
     */
    Three = 3
}

export interface WorkspaceConfiguration {

    /**
     * Return a value from this configuration.
     *
     * @param section Configuration name, supports _dotted_ names.
     * @return The value `section` denotes or `undefined`.
     */
    get<T>(section: string): T | undefined;

    /**
     * Return a value from this configuration.
     *
     * @param section Configuration name, supports _dotted_ names.
     * @param defaultValue A value should be returned when no value could be found, is `undefined`.
     * @return The value `section` denotes or the default.
     */
    get<T>(section: string, defaultValue: T): T;

    /**
     * Check if this configuration has a certain value.
     *
     * @param section Configuration name, supports _dotted_ names.
     * @return `true` if the section doesn't resolve to `undefined`.
     */
    has(section: string): boolean;

    /**
     * Retrieve all information about a configuration setting. A configuration value
     * often consists of a *default* value, a global or installation-wide value,
     * a workspace-specific value and a folder-specific value.
     *
     * The *effective* value (returned by [`get`](#WorkspaceConfiguration.get))
     * is computed like this: `defaultValue` overwritten by `globalValue`,
     * `globalValue` overwritten by `workspaceValue`. `workspaceValue` overwritten by `workspaceFolderValue`.
     * Refer to [Settings Inheritance](https://code.visualstudio.com/docs/getstarted/settings)
     * for more information.
     *
     * *Note:* The configuration name must denote a leaf in the configuration tree
     * (`editor.fontSize` vs `editor`) otherwise no result is returned.
     *
     * @param section Configuration name, supports _dotted_ names.
     * @return Information about a configuration setting or `undefined`.
     */
    inspect<T>(section: string): { key: string; defaultValue?: T; globalValue?: T; workspaceValue?: T, workspaceFolderValue?: T } | undefined;

    /**
     * Update a configuration value. The updated configuration values are persisted.
     *
     * A value can be changed in
     *
     * - [Global configuration](#ConfigurationTarget.Global): Changes the value for all instances of the editor.
     * - [Workspace configuration](#ConfigurationTarget.Workspace): Changes the value for current workspace, if available.
     * - [Workspace folder configuration](#ConfigurationTarget.WorkspaceFolder): Changes the value for the
     * [Workspace folder](#workspace.workspaceFolders) to which the current [configuration](#WorkspaceConfiguration) is scoped to.
     *
     * *Note 1:* Setting a global value in the presence of a more specific workspace value
     * has no observable effect in that workspace, but in others. Setting a workspace value
     * in the presence of a more specific folder value has no observable effect for the resources
     * under respective [folder](#workspace.workspaceFolders), but in others. Refer to
     * [Settings Inheritance](https://code.visualstudio.com/docs/getstarted/settings) for more information.
     *
     * *Note 2:* To remove a configuration value use `undefined`, like so: `config.update('somekey', undefined)`
     *
     * Will throw error when
     * - Writing a configuration which is not registered.
     * - Writing a configuration to workspace or folder target when no workspace is opened
     * - Writing a configuration to folder target when there is no folder settings
     * - Writing to folder target without passing a resource when getting the configuration (`workspace.getConfiguration(section, resource)`)
     * - Writing a window configuration to folder target
     *
     * @param section Configuration name, supports _dotted_ names.
     * @param value The new value.
     * @param configurationTarget The [configuration target](#ConfigurationTarget) or a boolean value.
     *	- If `true` configuration target is `ConfigurationTarget.Global`.
     *	- If `false` configuration target is `ConfigurationTarget.Workspace`.
     *	- If `undefined` or `null` configuration target is
     *	`ConfigurationTarget.WorkspaceFolder` when configuration is resource specific
     *	`ConfigurationTarget.Workspace` otherwise.
     */
    update(section: string, value: any, configurationTarget?: ConfigurationTarget | boolean): Thenable<void>;

    /**
     * Readable dictionary that backs this configuration.
     */
    readonly [key: string]: any;
}

/**
 * The configuration target
 */
export enum ConfigurationTarget {
    /**
     * Global configuration
    */
    Global = 1,

    /**
     * Workspace configuration
     */
    Workspace = 2,

    /**
     * Workspace folder configuration
     */
    WorkspaceFolder = 3
}

/**
 * Represents the alignment of status bar items.
 */
export enum StatusBarAlignment {

    /**
     * Aligned to the left side.
     */
    Left = 1,

    /**
     * Aligned to the right side.
     */
    Right = 2
}


export interface StatusBarItem {

    /**
     * The alignment of this item.
     */
    readonly alignment: StatusBarAlignment;

    /**
     * The priority of this item. Higher value means the item should
     * be shown more to the left.
     */
    readonly priority: number;

    /**
     * The text to show for the entry. You can embed icons in the text by leveraging the syntax:
     *
     * `My text $(icon-name) contains icons like $(icon'name) this one.`
     *
     * Where the icon-name is taken from the [octicon](https://octicons.github.com) icon set, e.g.
     * `light-bulb`, `thumbsup`, `zap` etc.
     */
    text: string;

    /**
     * The tooltip text when you hover over this entry.
     */
    tooltip: string | undefined;

    /**
     * The foreground color for this entry.
     */
    color: string | undefined;

    /**
     * The identifier of a command to run on click. The command must be
     * [known](#commands.getCommands).
     */
    command: string | undefined;

    /**
     * Shows the entry in the status bar.
     */
    show(): void;

    /**
     * Hide the entry in the status bar.
     */
    hide(): void;

    /**
     * Dispose and free associated resources. Call
     * [hide](#StatusBarItem.hide).
     */
    dispose(): void;
}

export interface Event<T> {
    /**
     * A function that represents an event to which you subscribe by calling it with
     * a listener function as argument.
     *
     * @param listener The listener function will be called when the event happens.
     * @param thisArgs The `this`-argument which will be used when calling the event listener.
     * @param disposables An array to which a [disposable](#Disposable) will be added.
     * @return A disposable which unsubscribes the event listener.
     */
    (listener: (e: T) => any, thisArgs?: any, disposables?: Disposable[]): Disposable;
}

export interface Disposable {
    /**
     * Dispose this object.
     */
    dispose(): any;
}


export interface CancellationToken {

    /**
     * Is `true` when the token has been cancelled, `false` otherwise.
     */
    isCancellationRequested: boolean;

    /**
     * An [event](#Event) which fires upon cancellation.
     */
    onCancellationRequested: Event<any>;
}


export interface DocumentFilter {

    /**
     * A language id, like `typescript`.
     */
    language?: string;

    /**
     * A Uri [scheme](#Uri.scheme), like `file` or `untitled`.
     */
    scheme?: string;

    /**
     * A [glob pattern](#GlobPattern) that is matched on the absolute path of the document. Use a [relative pattern](#RelativePattern)
     * to filter documents to a [workspace folder](#WorkspaceFolder).
     */
    pattern?: GlobPattern;
}

export type GlobPattern = string;

export type DocumentSelector = string | DocumentFilter | (string | DocumentFilter)[];

export interface MessageOptions {

    /**
     * Indicates that this message should be modal.
     */
    modal?: boolean;
}

export interface TextEditor {

    /**
     * The document associated with this text editor. The document will be the same for the entire lifetime of this text editor.
     */
    document: TextDocument;
}

/**
	 * A universal resource identifier representing either a file on disk
	 * or another resource, like untitled resources.
	 */
export interface Uri {

    /**
     * Create an URI from a file system path. The [scheme](#Uri.scheme)
     * will be `file`.
     *
     * @param path A file system or UNC path.
     * @return A new Uri instance.
     */

    /**
     * Create an URI from a string. Will throw if the given value is not
     * valid.
     *
     * @param value The string value of an Uri.
     * @return A new Uri instance.
     */
    /**
     * Scheme is the `http` part of `http://www.msft.com/some/path?query#fragment`.
     * The part before the first colon.
     */
    readonly scheme: string;

    /**
     * Authority is the `www.msft.com` part of `http://www.msft.com/some/path?query#fragment`.
     * The part between the first double slashes and the next slash.
     */
    readonly authority: string;

    /**
     * Path is the `/some/path` part of `http://www.msft.com/some/path?query#fragment`.
     */
    readonly path: string;

    /**
     * Query is the `query` part of `http://www.msft.com/some/path?query#fragment`.
     */
    readonly query: string;

    /**
     * Fragment is the `fragment` part of `http://www.msft.com/some/path?query#fragment`.
     */
    readonly fragment: string;

    /**
     * The string representing the corresponding file system path of this Uri.
     *
     * Will handle UNC paths and normalize windows drive letters to lower-case. Also
     * uses the platform specific path separator. Will *not* validate the path for
     * invalid characters and semantics. Will *not* look at the scheme of this Uri.
     */
    readonly fsPath: string;

    /**
     * Derive a new Uri from this Uri.
     *
     * ```ts
     * let file = Uri.parse('before:some/file/path');
     * let other = file.with({ scheme: 'after' });
     * assert.ok(other.toString() === 'after:some/file/path');
     * ```
     *
     * @param change An object that describes a change to this Uri. To unset components use `null` or
     *  the empty string.
     * @return A new Uri that reflects the given change. Will return `this` Uri if the change
     *  is not changing anything.
     */
    with(change: { scheme?: string; authority?: string; path?: string; query?: string; fragment?: string }): Uri;

    /**
     * Returns a string representation of this Uri. The representation and normalization
     * of a URI depends on the scheme. The resulting string can be safely used with
     * [Uri.parse](#Uri.parse).
     *
     * @param skipEncoding Do not percentage-encode the result, defaults to `false`. Note that
     *	the `#` and `?` characters occurring in the path will always be encoded.
     * @returns A string representation of this Uri.
     */
    toString(skipEncoding?: boolean): string;

    /**
     * Returns a JSON representation of this Uri.
     *
     * @return An object.
     */
    toJSON(): any;
}

export interface MessageItem {

    /**
     * A short title like 'Retry', 'Open Log' etc.
     */
    title: string;

    /**
     * Indicates that this item replaces the default
     * 'Close' action.
     */
    isCloseAffordance?: boolean;
}

/**
 * Represents a text document, such as a source file. Text documents have
 * [lines](#TextLine) and knowledge about an underlying resource like a file.
 */
export interface TextDocument {

    /**
     * The associated URI for this document. Most documents have the __file__-scheme, indicating that they
     * represent files on disk. However, some documents may have other schemes indicating that they are not
     * available on disk.
     */
    readonly uri: Uri;

    /**
     * The file system path of the associated resource. Shorthand
     * notation for [TextDocument.uri.fsPath](#TextDocument.uri). Independent of the uri scheme.
     */
    readonly fileName: string;

    /**
     * Is this document representing an untitled file.
     */
    readonly isUntitled: boolean;

    /**
     * The identifier of the language associated with this document.
     */
    readonly languageId: string;

    /**
     * The version number of this document (it will strictly increase after each
     * change, including undo/redo).
     */
    readonly version: number;

    /**
     * `true` if there are unpersisted changes.
     */
    readonly isDirty: boolean;

    /**
     * `true` if the document have been closed. A closed document isn't synchronized anymore
     * and won't be re-used when the same resource is opened again.
     */
    readonly isClosed: boolean;

    /**
     * Save the underlying file.
     *
     * @return A promise that will resolve to true when the file
     * has been saved. If the file was not dirty or the save failed,
     * will return false.
     */
    save(): Thenable<boolean>;

    /**
     * The [end of line](#EndOfLine) sequence that is predominately
     * used in this document.
     */
    readonly eol: EndOfLine;

    /**
     * The number of lines in this document.
     */
    readonly lineCount: number;

    /**
     * Returns a text line denoted by the line number. Note
     * that the returned object is *not* live and changes to the
     * document are not reflected.
     *
     * @param line A line number in [0, lineCount).
     * @return A [line](#TextLine).
     */
    lineAt(line: number): TextLine;

    /**
     * Returns a text line denoted by the position. Note
     * that the returned object is *not* live and changes to the
     * document are not reflected.
     *
     * The position will be [adjusted](#TextDocument.validatePosition).
     *
     * @see [TextDocument.lineAt](#TextDocument.lineAt)
     * @param position A position.
     * @return A [line](#TextLine).
     */
    lineAt(position: Position): TextLine;

    /**
     * Converts the position to a zero-based offset.
     *
     * The position will be [adjusted](#TextDocument.validatePosition).
     *
     * @param position A position.
     * @return A valid zero-based offset.
     */
    offsetAt(position: Position): number;

    /**
     * Converts a zero-based offset to a position.
     *
     * @param offset A zero-based offset.
     * @return A valid [position](#Position).
     */
    positionAt(offset: number): Position;

    /**
     * Get the text of this document. A substring can be retrieved by providing
     * a range. The range will be [adjusted](#TextDocument.validateRange).
     *
     * @param range Include only the text included by the range.
     * @return The text inside the provided range or the entire text.
     */
    getText(range?: Range): string;

    /**
     * Get a word-range at the given position. By default words are defined by
     * common separators, like space, -, _, etc. In addition, per language custom
     * [word definitions](#LanguageConfiguration.wordPattern) can be defined. It
     * is also possible to provide a custom regular expression.
     *
     * * *Note 1:* A custom regular expression must not match the empty string and
     * if it does, it will be ignored.
     * * *Note 2:* A custom regular expression will fail to match multiline strings
     * and in the name of speed regular expressions should not match words with
     * spaces. Use [`TextLine.text`](#TextLine.text) for more complex, non-wordy, scenarios.
     *
     * The position will be [adjusted](#TextDocument.validatePosition).
     *
     * @param position A position.
     * @param regex Optional regular expression that describes what a word is.
     * @return A range spanning a word, or `undefined`.
     */
    getWordRangeAtPosition(position: Position, regex?: RegExp): Range | undefined;

    /**
     * Ensure a range is completely contained in this document.
     *
     * @param range A range.
     * @return The given range or a new, adjusted range.
     */
    validateRange(range: Range): Range;

    /**
     * Ensure a position is contained in the range of this document.
     *
     * @param position A position.
     * @return The given position or a new, adjusted position.
     */
    validatePosition(position: Position): Position;
}

/**
 * Represents an end of line character sequence in a [document](#TextDocument).
 */
export enum EndOfLine {
    /**
     * The line feed `\n` character.
     */
    LF = 1,
    /**
     * The carriage return line feed `\r\n` sequence.
     */
    CRLF = 2
}

/**
 * Represents a line and character position, such as
 * the position of the cursor.
 *
 * Position objects are __immutable__. Use the [with](#Position.with) or
 * [translate](#Position.translate) methods to derive new positions
 * from an existing position.
 */
export interface Position {

    /**
     * The zero-based line value.
     */
    readonly line: number;

    /**
     * The zero-based character value.
     */
    readonly character: number;

    /**
     * @param line A zero-based line value.
     * @param character A zero-based character value.
     */

    /**
     * Check if `other` is before this position.
     *
     * @param other A position.
     * @return `true` if position is on a smaller line
     * or on the same line on a smaller character.
     */
    isBefore(other: Position): boolean;

    /**
     * Check if `other` is before or equal to this position.
     *
     * @param other A position.
     * @return `true` if position is on a smaller line
     * or on the same line on a smaller or equal character.
     */
    isBeforeOrEqual(other: Position): boolean;

    /**
     * Check if `other` is after this position.
     *
     * @param other A position.
     * @return `true` if position is on a greater line
     * or on the same line on a greater character.
     */
    isAfter(other: Position): boolean;

    /**
     * Check if `other` is after or equal to this position.
     *
     * @param other A position.
     * @return `true` if position is on a greater line
     * or on the same line on a greater or equal character.
     */
    isAfterOrEqual(other: Position): boolean;

    /**
     * Check if `other` equals this position.
     *
     * @param other A position.
     * @return `true` if the line and character of the given position are equal to
     * the line and character of this position.
     */
    isEqual(other: Position): boolean;

    /**
     * Compare this to `other`.
     *
     * @param other A position.
     * @return A number smaller than zero if this position is before the given position,
     * a number greater than zero if this position is after the given position, or zero when
     * this and the given position are equal.
     */
    compareTo(other: Position): number;

    /**
     * Create a new position relative to this position.
     *
     * @param lineDelta Delta value for the line value, default is `0`.
     * @param characterDelta Delta value for the character value, default is `0`.
     * @return A position which line and character is the sum of the current line and
     * character and the corresponding deltas.
     */
    translate(lineDelta?: number, characterDelta?: number): Position;

    /**
     * Derived a new position relative to this position.
     *
     * @param change An object that describes a delta to this position.
     * @return A position that reflects the given delta. Will return `this` position if the change
     * is not changing anything.
     */
    translate(change: { lineDelta?: number; characterDelta?: number; }): Position;

    /**
     * Create a new position derived from this position.
     *
     * @param line Value that should be used as line value, default is the [existing value](#Position.line)
     * @param character Value that should be used as character value, default is the [existing value](#Position.character)
     * @return A position where line and character are replaced by the given values.
     */
    with(line?: number, character?: number): Position;

    /**
     * Derived a new position from this position.
     *
     * @param change An object that describes a change to this position.
     * @return A position that reflects the given change. Will return `this` position if the change
     * is not changing anything.
     */
    with(change: { line?: number; character?: number; }): Position;
}

export interface Range {

    /**
     * The start position. It is before or equal to [end](#Range.end).
     */
    readonly start: Position;

    /**
     * The end position. It is after or equal to [start](#Range.start).
     */
    readonly end: Position;

    /**
     * `true` if `start` and `end` are equal.
     */
    isEmpty: boolean;

    /**
     * `true` if `start.line` and `end.line` are equal.
     */
    isSingleLine: boolean;

    /**
     * Check if a position or a range is contained in this range.
     *
     * @param positionOrRange A position or a range.
     * @return `true` if the position or range is inside or equal
     * to this range.
     */
    contains(positionOrRange: Position | Range): boolean;

    /**
     * Check if `other` equals this range.
     *
     * @param other A range.
     * @return `true` when start and end are [equal](#Position.isEqual) to
     * start and end of this range.
     */
    isEqual(other: Range): boolean;

    /**
     * Intersect `range` with this range and returns a new range or `undefined`
     * if the ranges have no overlap.
     *
     * @param range A range.
     * @return A range of the greater start and smaller end positions. Will
     * return undefined when there is no overlap.
     */
    intersection(range: Range): Range | undefined;

    /**
     * Compute the union of `other` with this range.
     *
     * @param other A range.
     * @return A range of smaller start position and the greater end position.
     */
    union(other: Range): Range;

    /**
     * Derived a new range from this range.
     *
     * @param start A position that should be used as start. The default value is the [current start](#Range.start).
     * @param end A position that should be used as end. The default value is the [current end](#Range.end).
     * @return A range derived from this range with the given start and end position.
     * If start and end are not different `this` range will be returned.
     */
    with(start?: Position, end?: Position): Range;

    /**
     * Derived a new range from this range.
     *
     * @param change An object that describes a change to this range.
     * @return A range that reflects the given change. Will return `this` range if the change
     * is not changing anything.
     */
    with(change: { start?: Position, end?: Position }): Range;
}

/**
 * Represents a line of text, such as a line of source code.
 *
 * TextLine objects are __immutable__. When a [document](#TextDocument) changes,
 * previously retrieved lines will not represent the latest state.
 */
export interface TextLine {

    /**
     * The zero-based line number.
     */
    readonly lineNumber: number;

    /**
     * The text of this line without the line separator characters.
     */
    readonly text: string;

    /**
     * The range this line covers without the line separator characters.
     */
    readonly range: Range;

    /**
     * The range this line covers with the line separator characters.
     */
    readonly rangeIncludingLineBreak: Range;

    /**
     * The offset of the first character which is not a whitespace character as defined
     * by `/\s/`. **Note** that if a line is all whitespace the length of the line is returned.
     */
    readonly firstNonWhitespaceCharacterIndex: number;

    /**
     * Whether this line is whitespace only, shorthand
     * for [TextLine.firstNonWhitespaceCharacterIndex](#TextLine.firstNonWhitespaceCharacterIndex) === [TextLine.text.length](#TextLine.text).
     */
    readonly isEmptyOrWhitespace: boolean;
}

export interface FileSystemWatcher extends Disposable {

    /**
     * true if this file system watcher has been created such that
     * it ignores creation file system events.
     */
    ignoreCreateEvents: boolean;

    /**
     * true if this file system watcher has been created such that
     * it ignores change file system events.
     */
    ignoreChangeEvents: boolean;

    /**
     * true if this file system watcher has been created such that
     * it ignores delete file system events.
     */
    ignoreDeleteEvents: boolean;

    /**
     * An event which fires on file/folder creation.
     */
    onDidCreate: Event<Uri>;

    /**
     * An event which fires on file/folder change.
     */
    onDidChange: Event<Uri>;

    /**
     * An event which fires on file/folder deletion.
     */
    onDidDelete: Event<Uri>;
}

export interface ConfigurationChangeEvent {

    /**
     * Returns `true` if the given section for the given resource (if provided) is affected.
     *
     * @param section Configuration name, supports _dotted_ names.
     * @param resource A resource Uri.
     * @return `true` if the given section for the given resource (if provided) is affected.
     */
    affectsConfiguration(section: string, resource?: Uri): boolean;
}

export interface WebviewPanelSerializer {
    /**
     * Restore a webview panel from its serialized `state`.
     *
     * Called when a serialized webview first becomes visible.
     *
     * @param webviewPanel Webview panel to restore. The serializer should take ownership of this panel. The
     * serializer must restore the webview's `.html` and hook up all webview events.
     * @param state Persisted state from the webview content.
     *
     * @return Thenable indicating that the webview has been fully restored.
     */
    deserializeWebviewPanel(webviewPanel: WebviewPanel, state: any): Thenable<void>;
}

/**
 * Content settings for a webview panel.
 */
export interface WebviewPanelOptions {
    /**
     * Controls if the find widget is enabled in the panel.
     *
     * Defaults to false.
     */
    readonly enableFindWidget?: boolean;

    /**
     * Controls if the webview panel's content (iframe) is kept around even when the panel
     * is no longer visible.
     *
     * Normally the webview panel's html context is created when the panel becomes visible
     * and destroyed when it is is hidden. Extensions that have complex state
     * or UI can set the `retainContextWhenHidden` to make VS Code keep the webview
     * context around, even when the webview moves to a background tab. When a webview using
     * `retainContextWhenHidden` becomes hidden, its scripts and other dynamic content are suspended.
     * When the panel becomes visible again, the context is automatically restored
     * in the exact same state it was in originally. You cannot send messages to a
     * hidden webview, even with `retainContextWhenHidden` enabled.
     *
     * `retainContextWhenHidden` has a high memory overhead and should only be used if
     * your panel's context cannot be quickly saved and restored.
     */
    readonly retainContextWhenHidden?: boolean;
}

/**
 * A panel that contains a webview.
 */
export interface WebviewPanel {
    /**
     * Identifies the type of the webview panel, such as `'markdown.preview'`.
     */
    readonly viewType: string;

    /**
     * Title of the panel shown in UI.
     */
    title: string;

    /**
     * Webview belonging to the panel.
     */
    readonly webview: Webview;

    /**
     * Content settings for the webview panel.
     */
    readonly options: WebviewPanelOptions;

    /**
     * Editor position of the panel. This property is only set if the webview is in
     * one of the editor view columns.
     *
     * @deprecated
     */
    readonly viewColumn?: ViewColumn;

    /**
     * Whether the panel is active (focused by the user).
     */
    readonly active: boolean;

    /**
     * Whether the panel is visible.
     */
    readonly visible: boolean;

    /**
     * Fired when the panel's view state changes.
     */
    readonly onDidChangeViewState: Event<WebviewPanelOnDidChangeViewStateEvent>;

    /**
     * Fired when the panel is disposed.
     *
     * This may be because the user closed the panel or because `.dispose()` was
     * called on it.
     *
     * Trying to use the panel after it has been disposed throws an exception.
     */
    readonly onDidDispose: Event<void>;

    /**
     * Show the webview panel in a given column.
     *
     * A webview panel may only show in a single column at a time. If it is already showing, this
     * method moves it to a new column.
     *
     * @param viewColumn View column to show the panel in. Shows in the current `viewColumn` if undefined.
     * @param preserveFocus When `true`, the webview will not take focus.
     */
    reveal(viewColumn?: ViewColumn, preserveFocus?: boolean): void;

    /**
     * Dispose of the webview panel.
     *
     * This closes the panel if it showing and disposes of the resources owned by the webview.
     * Webview panels are also disposed when the user closes the webview panel. Both cases
     * fire the `onDispose` event.
     */
    dispose(): any;
}

/**
 * Event fired when a webview panel's view state changes.
 */
export interface WebviewPanelOnDidChangeViewStateEvent {
    /**
     * Webview panel whose view state changed.
     */
    readonly webviewPanel: WebviewPanel;
}

/**
 * A webview displays html content, like an iframe.
 */
export interface Webview {
    /**
     * Content settings for the webview.
     */
    options: WebviewOptions;

    /**
     * Contents of the webview.
     *
     * Should be a complete html document.
     */
    html: string;

    /**
     * Fired when the webview content posts a message.
     */
    readonly onDidReceiveMessage: Event<any>;

    /**
     * Post a message to the webview content.
     *
     * Messages are only delivered if the webview is visible.
     *
     * @param message Body of the message.
     */
    postMessage(message: any): Thenable<boolean>;

    /**
     * Convert a uri for the local file system to one that can be used inside webviews.
     *
     * Webviews cannot directly load resources from the workspace or local file system using `file:` uris. The
     * `asWebviewUri` function takes a local `file:` uri and converts it into a uri that can be used inside of
     * a webview to load the same resource:
     *
     * ```ts
     * webview.html = `<img src="${webview.asWebviewUri(vscode.Uri.file('/Users/codey/workspace/cat.gif'))}">`
     * ```
     */
    asWebviewUri(localResource: Uri): Uri;

    /**
     * Content security policy source for webview resources.
     *
     * This is the origin that should be used in a content security policy rule:
     *
     * ```
     * img-src https: ${webview.cspSource} ...;
     * ```
     */
    readonly cspSource: string;
}

/**
 * Content settings for a webview.
 */
export interface WebviewOptions {
    /**
     * Controls whether scripts are enabled in the webview content or not.
     *
     * Defaults to false (scripts-disabled).
     */
    readonly enableScripts?: boolean;

    /**
     * Controls whether command uris are enabled in webview content or not.
     *
     * Defaults to false.
     */
    readonly enableCommandUris?: boolean;

    /**
     * Root paths from which the webview can load local (filesystem) resources using the `vscode-resource:` scheme.
     *
     * Default to the root folders of the current workspace plus the extension's install directory.
     *
     * Pass in an empty array to disallow access to any local resources.
     */
    readonly localResourceRoots?: ReadonlyArray<Uri>;
}

/**
 * Thenable is a common denominator between ES6 promises, Q, jquery.Deferred, WinJS.Promise,
 * and others. This API makes no assumption about what promise library is being used which
 * enables reusing existing code without migrating to a specific promise implementation. Still,
 * we recommend the use of native promises which are available in this editor.
 */
export interface Thenable<T> {
    /**
	* Attaches callbacks for the resolution and/or rejection of the Promise.
	* @param onfulfilled The callback to execute when the Promise is resolved.
	* @param onrejected The callback to execute when the Promise is rejected.
	* @returns A Promise for the completion of which ever callback is executed.
	*/
    then<TResult>(onfulfilled?: (value: T) => TResult | Thenable<TResult>, onrejected?: (reason: any) => TResult | Thenable<TResult>): Thenable<TResult>;
    then<TResult>(onfulfilled?: (value: T) => TResult | Thenable<TResult>, onrejected?: (reason: any) => void): Thenable<TResult>;
}

export interface Extension<T> {
    readonly id: string;
    readonly packageJSON: any;
}

/**
 * Represents semantic tokens, either in a range or in an entire document.
 * @see [provideDocumentSemanticTokens](#DocumentSemanticTokensProvider.provideDocumentSemanticTokens) for an explanation of the format.
 * @see [SemanticTokensBuilder](#SemanticTokensBuilder) for a helper to create an instance.
 */
export class SemanticTokens {
    /**
     * The result id of the tokens.
     *
     * This is the id that will be passed to `DocumentSemanticTokensProvider.provideDocumentSemanticTokensEdits` (if implemented).
     */
    readonly resultId?: string;
    /**
     * The actual tokens data.
     * @see [provideDocumentSemanticTokens](#DocumentSemanticTokensProvider.provideDocumentSemanticTokens) for an explanation of the format.
     */
    readonly data: Uint32Array;

    constructor(data: Uint32Array, resultId?: string) {
        this.data = data;
        this.resultId = resultId;
    }
}

/**
 * Represents edits to semantic tokens.
 * @see [provideDocumentSemanticTokensEdits](#DocumentSemanticTokensProvider.provideDocumentSemanticTokensEdits) for an explanation of the format.
 */
export class SemanticTokensEdits {
    /**
     * The result id of the tokens.
     *
     * This is the id that will be passed to `DocumentSemanticTokensProvider.provideDocumentSemanticTokensEdits` (if implemented).
     */
    readonly resultId?: string;
    /**
     * The edits to the tokens data.
     * All edits refer to the initial data state.
     */
    readonly edits: SemanticTokensEdit[];

    constructor(edits: SemanticTokensEdit[], resultId?: string) {
        this.edits = edits;
        this.resultId = resultId;
    }
}

/**
 * Represents an edit to semantic tokens.
 * @see [provideDocumentSemanticTokensEdits](#DocumentSemanticTokensProvider.provideDocumentSemanticTokensEdits) for an explanation of the format.
 */
export class SemanticTokensEdit {
    /**
     * The start offset of the edit.
     */
    readonly start: number;
    /**
     * The count of elements to remove.
     */
    readonly deleteCount: number;
    /**
     * The elements to insert.
     */
    readonly data?: Uint32Array;

    constructor(start: number, deleteCount: number, data?: Uint32Array) {
        this.start = start;
        this.deleteCount = deleteCount;
        this.data = data;
    }
}

export interface SemanticTokensLegend {
    readonly tokenTypes: string[];
    readonly tokenModifiers: string[];
}

/**
 * The document semantic tokens provider interface defines the contract between extensions and
 * semantic tokens.
 */
export interface DocumentSemanticTokensProvider {
    /**
     * An optional event to signal that the semantic tokens from this provider have changed.
     */
    onDidChangeSemanticTokens?: Event<void>;

    /**
     * Tokens in a file are represented as an array of integers. The position of each token is expressed relative to
     * the token before it, because most tokens remain stable relative to each other when edits are made in a file.
     *
     * ---
     * In short, each token takes 5 integers to represent, so a specific token `i` in the file consists of the following array indices:
     *  - at index `5*i`   - `deltaLine`: token line number, relative to the previous token
     *  - at index `5*i+1` - `deltaStart`: token start character, relative to the previous token (relative to 0 or the previous token's start if they are on the same line)
     *  - at index `5*i+2` - `length`: the length of the token. A token cannot be multiline.
     *  - at index `5*i+3` - `tokenType`: will be looked up in `SemanticTokensLegend.tokenTypes`. We currently ask that `tokenType` < 65536.
     *  - at index `5*i+4` - `tokenModifiers`: each set bit will be looked up in `SemanticTokensLegend.tokenModifiers`
     *
     * ---
     * ### How to encode tokens
     *
     * Here is an example for encoding a file with 3 tokens in a uint32 array:
     * ```
     *    { line: 2, startChar:  5, length: 3, tokenType: "property",  tokenModifiers: ["private", "static"] },
     *    { line: 2, startChar: 10, length: 4, tokenType: "type",      tokenModifiers: [] },
     *    { line: 5, startChar:  2, length: 7, tokenType: "class",     tokenModifiers: [] }
     * ```
     *
     * 1. First of all, a legend must be devised. This legend must be provided up-front and capture all possible token types.
     * For this example, we will choose the following legend which must be passed in when registering the provider:
     * ```
     *    tokenTypes: ['property', 'type', 'class'],
     *    tokenModifiers: ['private', 'static']
     * ```
     *
     * 2. The first transformation step is to encode `tokenType` and `tokenModifiers` as integers using the legend. Token types are looked
     * up by index, so a `tokenType` value of `1` means `tokenTypes[1]`. Multiple token modifiers can be set by using bit flags,
     * so a `tokenModifier` value of `3` is first viewed as binary `0b00000011`, which means `[tokenModifiers[0], tokenModifiers[1]]` because
     * bits 0 and 1 are set. Using this legend, the tokens now are:
     * ```
     *    { line: 2, startChar:  5, length: 3, tokenType: 0, tokenModifiers: 3 },
     *    { line: 2, startChar: 10, length: 4, tokenType: 1, tokenModifiers: 0 },
     *    { line: 5, startChar:  2, length: 7, tokenType: 2, tokenModifiers: 0 }
     * ```
     *
     * 3. The next step is to represent each token relative to the previous token in the file. In this case, the second token
     * is on the same line as the first token, so the `startChar` of the second token is made relative to the `startChar`
     * of the first token, so it will be `10 - 5`. The third token is on a different line than the second token, so the
     * `startChar` of the third token will not be altered:
     * ```
     *    { deltaLine: 2, deltaStartChar: 5, length: 3, tokenType: 0, tokenModifiers: 3 },
     *    { deltaLine: 0, deltaStartChar: 5, length: 4, tokenType: 1, tokenModifiers: 0 },
     *    { deltaLine: 3, deltaStartChar: 2, length: 7, tokenType: 2, tokenModifiers: 0 }
     * ```
     *
     * 4. Finally, the last step is to inline each of the 5 fields for a token in a single array, which is a memory friendly representation:
     * ```
     *    // 1st token,  2nd token,  3rd token
     *    [  2,5,3,0,3,  0,5,4,1,0,  3,2,7,2,0 ]
     * ```
     *
     * @see [SemanticTokensBuilder](#SemanticTokensBuilder) for a helper to encode tokens as integers.
     * *NOTE*: When doing edits, it is possible that multiple edits occur until VS Code decides to invoke the semantic tokens provider.
     * *NOTE*: If the provider cannot temporarily compute semantic tokens, it can indicate this by throwing an error with the message 'Busy'.
     */
    provideDocumentSemanticTokens(document: TextDocument, token: CancellationToken): ProviderResult<SemanticTokens>;

    /**
     * Instead of always returning all the tokens in a file, it is possible for a `DocumentSemanticTokensProvider` to implement
     * this method (`provideDocumentSemanticTokensEdits`) and then return incremental updates to the previously provided semantic tokens.
     *
     * ---
     * ### How tokens change when the document changes
     *
     * Suppose that `provideDocumentSemanticTokens` has previously returned the following semantic tokens:
     * ```
     *    // 1st token,  2nd token,  3rd token
     *    [  2,5,3,0,3,  0,5,4,1,0,  3,2,7,2,0 ]
     * ```
     *
     * Also suppose that after some edits, the new semantic tokens in a file are:
     * ```
     *    // 1st token,  2nd token,  3rd token
     *    [  3,5,3,0,3,  0,5,4,1,0,  3,2,7,2,0 ]
     * ```
     * It is possible to express these new tokens in terms of an edit applied to the previous tokens:
     * ```
     *    [  2,5,3,0,3,  0,5,4,1,0,  3,2,7,2,0 ] // old tokens
     *    [  3,5,3,0,3,  0,5,4,1,0,  3,2,7,2,0 ] // new tokens
     *
     *    edit: { start:  0, deleteCount: 1, data: [3] } // replace integer at offset 0 with 3
     * ```
     *
     * *NOTE*: If the provider cannot compute `SemanticTokensEdits`, it can "give up" and return all the tokens in the document again.
     * *NOTE*: All edits in `SemanticTokensEdits` contain indices in the old integers array, so they all refer to the previous result state.
     */
    provideDocumentSemanticTokensEdits?(document: TextDocument, previousResultId: string, token: CancellationToken): ProviderResult<SemanticTokens | SemanticTokensEdits>;
}

/**
 * The document range semantic tokens provider interface defines the contract between extensions and
 * semantic tokens.
 */
export interface DocumentRangeSemanticTokensProvider {
    /**
     * @see [provideDocumentSemanticTokens](#DocumentSemanticTokensProvider.provideDocumentSemanticTokens).
     */
    provideDocumentRangeSemanticTokens(document: TextDocument, range: Range, token: CancellationToken): ProviderResult<SemanticTokens>;
}

export interface api {
    commands: {
        executeCommand: <T>(command: string, ...rest: any[]) => Thenable<T | undefined>;
        registerCommand(command: string, callback: (...args: any[]) => any, thisArg?: any): Disposable;
    };
    languages: {
        match: (selector: DocumentSelector, document: TextDocument) => number;
        /**
        * Register a semantic tokens provider for a whole document.
        *
        * Multiple providers can be registered for a language. In that case providers are sorted
        * by their [score](#languages.match) and the best-matching provider is used. Failure
        * of the selected provider will cause a failure of the whole operation.
        *
        * @param selector A selector that defines the documents this provider is applicable to.
        * @param provider A document semantic tokens provider.
        * @return A [disposable](#Disposable) that unregisters this provider when being disposed.
        */
       registerDocumentSemanticTokensProvider(selector: DocumentSelector, provider: DocumentSemanticTokensProvider, legend: SemanticTokensLegend): Disposable;

       /**
        * Register a semantic tokens provider for a document range.
        *
        * *Note:* If a document has both a `DocumentSemanticTokensProvider` and a `DocumentRangeSemanticTokensProvider`,
        * the range provider will be invoked only initially, for the time in which the full document provider takes
        * to resolve the first request. Once the full document provider resolves the first request, the semantic tokens
        * provided via the range provider will be discarded and from that point forward, only the document provider
        * will be used.
        *
        * Multiple providers can be registered for a language. In that case providers are sorted
        * by their [score](#languages.match) and the best-matching provider is used. Failure
        * of the selected provider will cause a failure of the whole operation.
        *
        * @param selector A selector that defines the documents this provider is applicable to.
        * @param provider A document range semantic tokens provider.
        * @return A [disposable](#Disposable) that unregisters this provider when being disposed.
        */
       registerDocumentRangeSemanticTokensProvider(selector: DocumentSelector, provider: DocumentRangeSemanticTokensProvider, legend: SemanticTokensLegend): Disposable;

    };
    window: {
        activeTextEditor: TextEditor | undefined;
        showInformationMessage: <T extends MessageItem>(message: string, ...items: T[]) => Thenable<T | undefined>;
        showWarningMessage: <T extends MessageItem>(message: string, ...items: T[]) => Thenable<T | undefined>;
        showErrorMessage(message: string, ...items: string[]): Thenable<string | undefined>;
        createOutputChannel(name: string): OutputChannel;
        registerWebviewPanelSerializer(viewType: string, serializer: WebviewPanelSerializer): Disposable;
    };
    workspace: {
        openTextDocument: (uri: Uri) => Thenable<TextDocument>;
        getConfiguration: (section?: string, resource?: Uri) => WorkspaceConfiguration;
        asRelativePath: (pathOrUri: string | Uri, includeWorkspaceFolder?: boolean) => string;
        createFileSystemWatcher(globPattern: GlobPattern, ignoreCreateEvents?: boolean, ignoreChangeEvents?: boolean, ignoreDeleteEvents?: boolean): FileSystemWatcher;
        onDidChangeConfiguration: Event<ConfigurationChangeEvent>;
    };
    extensions: {
        getExtension(extensionId: string): Extension<any> | undefined;
        all: ReadonlyArray<Extension<any>>;
    };
    Uri: {
        parse(value: string): Uri;
    };
    Disposable: {
        from(...disposableLikes: { dispose: () => any }[]): Disposable;
    };

    version: string;
}