# Info

In practice the Razor VSCode extension operates as a library that [OmniSharp](https://github.com/omnisharp/omnisharp-vscode) (O#) bootstraps and includes in its operation flow. Therefore, in order to provide a dev experience for testing out Razor VSCode extension changes this project attempts to replicate what O# does to bootstrap Razor's VSCode extension library.

## Debugging

1. npm install -g typescript
1. npm install -g yarn
1. .\build.cmd

### Debugging with Omnisharp-vscode

If you need to make changes to both the Razor-vscode extension and the Omnisharp-vscode extension you might find it useful to debug through both of them at the same time.

1. Do all the steps for debugging this repo
1. Clone https://github.com/OmniSharp/omnisharp-vscode
1. In that repo do all the steps to debug except pressing F5.
1. Edit https://github.com/OmniSharp/omnisharp-vscode/blob/master/.vscode/launch.json#L10 to include a second `--extensionDevelopmentPath` which points to our workspace.
1. F5 on their repo.
