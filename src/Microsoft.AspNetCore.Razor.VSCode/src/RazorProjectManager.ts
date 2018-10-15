/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as fs from 'fs';
import * as vscode from 'vscode';
import { IRazorProject } from './IRazorProject';
import { IRazorProjectChangeEvent } from './IRazorProjectChangeEvent';
import { IRazorProjectConfiguration } from './IRazorProjectConfiguration';
import { RazorLogger } from './RazorLogger';
import { RazorProjectChangeKind } from './RazorProjectChangeKind';
import { getUriPath } from './UriPaths';

const configurationFileGlobbingPath = `**/project.razor.json`;
const csprojGlobbingPath = `**/*.csproj`;

export class RazorProjectManager {
    private readonly razorProjects: { [csprojPath: string]: IRazorProject } = {};
    private onChangeEmitter = new vscode.EventEmitter<IRazorProjectChangeEvent>();

    constructor(private readonly logger: RazorLogger) {
    }

    public get onChange() { return this.onChangeEmitter.event; }

    public async initialize() {
        // Track current projects
        const projectUris = await vscode.workspace.findFiles(csprojGlobbingPath);
        for (const uri of projectUris) {
            this.addProject(uri);
        }

        const projectConfigurationUris = await vscode.workspace.findFiles(configurationFileGlobbingPath);
        for (const configurationUri of projectConfigurationUris) {
            this.updateProjectConfiguration(configurationUri);
        }
    }

    public register() {
        // Track future projects
        const projectWatcher = vscode.workspace.createFileSystemWatcher(csprojGlobbingPath);
        const didCreateRegistration = projectWatcher.onDidCreate(
            async (uri: vscode.Uri) => this.addProject(uri));
        const didDeleteRegistration = projectWatcher.onDidDelete(
            async (uri: vscode.Uri) => this.removeProject(uri));

        // Track future projects data files
        const configurationWatcher = vscode.workspace.createFileSystemWatcher(configurationFileGlobbingPath);
        const didCreateConfigRegistration = configurationWatcher.onDidCreate(
            async (uri: vscode.Uri) => this.updateProjectConfiguration(uri));
        const didDeleteConfigRegistration = configurationWatcher.onDidDelete(
            async (uri: vscode.Uri) => this.removeProjectConfiguration(uri));
        const didChangeConfigRegistration = configurationWatcher.onDidChange(
            async (uri: vscode.Uri) => this.updateProjectConfiguration(uri));

        return vscode.Disposable.from(
            configurationWatcher,
            didCreateRegistration,
            didDeleteRegistration,
            didCreateConfigRegistration,
            didDeleteConfigRegistration,
            didChangeConfigRegistration);
    }

    private updateProjectConfiguration(configurationUri: vscode.Uri) {
        const configuration = this.getProjectConfiguration(configurationUri);
        if (!configuration) {
            return;
        }

        if (!this.razorProjects[configuration.projectPath]) {
            this.logger.logVerbose(
                `Invalid project config. Could not find a corresponding project for ${configuration.projectPath}`);
            return;
        }

        const projectContainer = this._getProject(configuration.projectUri);
        const newProject: IRazorProject = {
            uri: projectContainer.uri,
            path: projectContainer.path,
            configuration,
        };
        this.razorProjects[newProject.path] = newProject;

        this.notifyProjectChange(newProject, RazorProjectChangeKind.changed);
    }

    private removeProjectConfiguration(uri: vscode.Uri) {
        const newProject = this.createDefaultProject(uri);
        this.razorProjects[newProject.path] = newProject;

        this.notifyProjectChange(newProject, RazorProjectChangeKind.changed);
    }

    private addProject(uri: vscode.Uri) {
        const project = this.createDefaultProject(uri);
        this.razorProjects[project.path] = project;

        this.notifyProjectChange(project, RazorProjectChangeKind.added);

        return project;
    }

    private removeProject(uri: vscode.Uri) {
        const project = this._getProject(uri);
        delete this.razorProjects[project.path];

        this.notifyProjectChange(project, RazorProjectChangeKind.removed);
    }

    private _getProject(uri: vscode.Uri) {
        const path = getUriPath(uri);
        const project = this.razorProjects[path];

        if (!project) {
            throw new Error('Requested project does not exist.');
        }

        return project;
    }

    private notifyProjectChange(project: IRazorProject, kind: RazorProjectChangeKind) {
        if (this.logger.verboseEnabled) {
            this.logger.logVerbose(
                `Notifying project '${getUriPath(project.uri)}' - '${RazorProjectChangeKind[kind]}'`);
        }

        const args: IRazorProjectChangeEvent = {
            project,
            kind,
        };

        this.onChangeEmitter.fire(args);
    }

    private getProjectConfiguration(uri: vscode.Uri) {
        const fileSystemPath = uri.fsPath || uri.path;
        try {
            const path = getUriPath(uri);
            const projectJson = fs.readFileSync(fileSystemPath, 'utf8');
            const lastUpdated = fs.statSync(fileSystemPath).mtime;
            const projectParsed = JSON.parse(projectJson);
            const projectUri = vscode.Uri.file(projectParsed.ProjectFilePath);
            const projectFilePath = getUriPath(projectUri);
            const configuration: IRazorProjectConfiguration = {
                uri,
                path,
                projectPath: projectFilePath,
                projectUri,
                configuration: projectParsed.Configuration,
                tagHelpers: projectParsed.TagHelpers,
                targetFramework: projectParsed.TargetFramework,
                lastUpdated,
            };
            return configuration;
        } catch (error) {
            this.logger.logError(`Failed to read project config at location ${fileSystemPath}: ${error}`);
        }

        return undefined;
    }

    private createDefaultProject(uri: vscode.Uri) {
        const path = getUriPath(uri);
        const project: IRazorProject = {
            uri,
            path,
        };

        return project;
    }
}
