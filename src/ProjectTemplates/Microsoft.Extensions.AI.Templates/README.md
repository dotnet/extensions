# Microsoft.Extensions.AI.Templates

Provides project templates for Microsoft.Extensions.AI.

## Updating project template dependencies

To update project template JavaScript dependencies:
1. Install a recent build of Node.js
2. Run the following commands from this directory:
    ```sh
    npm install
    npm run copy-dependencies
    ```

If you need to add a new dependency, run `npm install <package-name>` and update the `scripts` section in `package.json` to specify how the new dependency should be copied into its template.
