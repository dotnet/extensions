# Template Sandbox

This directory is used for debugging template execution tests. To debug a template:

1. Copy the contents of the template you want to test from `src/ProjectTemplates/Microsoft.Agents.AI.Templates/src/<TemplateName>` to this directory.
2. Run `dotnet new install .` from this directory to install the template locally.
3. Create a new directory (e.g., `output/<test-name>`) and run `dotnet new <template-short-name>` with desired parameters.
4. Debug and iterate on the template as needed.
5. When done, run `dotnet new uninstall .` to uninstall the local template.

The `output` directory is git-ignored and can be used for template instantiation testing.
