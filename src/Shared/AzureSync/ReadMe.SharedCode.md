The code in this directory is shared between dotnet/extensions and azure/dotnet-extensions-experimental. This contains shared sources used in both repos. Any changes to this dir need to be checked into both repositories.

dotnet/extensions code paths:
- extensions\src\shared\

azure/dotnet-extensions-experimental code paths:
- dotnet-extensions-experimental\src\shared\

## Copying code
- To copy code from dotnet/extensions to azure/dotnet-extensions-experimental, set AZUREEXTENSIONS_REPO to the azure repo root and then run CopyToAzure.cmd.
- To copy code from azure/dotnet-extensions-experimental to dotnet/extensions, set DOTNETEXTENSIONS_REPO to the dotnet repo root and then run CopyToDotnet.cmd.

## GitHub Actions

In azure/dotnet-extensions-experimental, the [dotnet-sync](https://github.com/Azure/dotnet-extensions-experimental/actions/workflows/dotnet-sync.yml) GitHub action automatically creates PRs to pull in changes from dotnet/runtime.

In dotnet/extensions, the [azure-sync](https://github.com/dotnet/extensions/actions/workflows/azure-sync.yml) GitHub action must be run **manually** to create PRs to pull in changes from azure/dotnet-extensions-experimental.
This is expected to be less common.
