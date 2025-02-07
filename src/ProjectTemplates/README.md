# Updating project template JavaScript dependencies

To update project template JavaScript dependencies:
1. Install a recent build of Node.js
2. Update the `package.json` file with added or updated dependencies
3. Run the following commands from this directory:
    ```sh
    npm install
    npm run copy-dependencies
    ```

To add a new dependency, run `npm install <package-name>` and update the `scripts` section in `package.json` to specify how the new dependency should be copied into its template.
