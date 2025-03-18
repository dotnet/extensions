### Instructions

Some of the tests in this project (such as the tests in `AzureResponseCacheTests.cs` and `AzureResultStoreTests.cs`)
require special configuration to run. These tests will be skipped by default if they have not been configured.

To configure the tests when running them locally on your machine, copy the `appsettings.json` file present in the
current folder to a new file named `appsettings.local.json`, and fill in the values for the following properties:

```
{
  "Configured": true,
  "StorageAccountEndpoint": "<end point url for an Azure Storage account that the tests can write to>",
  "StorageContainerName": "<name of a storage container within the above account that the tests can write to>"
}
```

The `appsettings.local.json` file is ignored via `.gitignore`, so it will not be committed when you make other changes
to the code in this repository.

You can also use the `ConfigureEvaluationTests.ps1` script available under the `scripts` folder at the root of this
repository to copy over previously created `appsettings.local.json` files to the current folder.
