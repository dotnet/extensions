### Instructions

Some of the tests in this project (such as the tests in `EvaluatorTests.cs`) require special configuration to run.
These tests will be skipped by default if they have not been configured.

To configure the tests when running them locally on your machine, copy the `appsettings.json` file present in the
current folder to a new file named `appsettings.local.json`, and fill in the values for the following properties:

```
{
  "Configured": true,
  "DeploymentName": "<the Azure Open AI model deployment that the tests should use>",
  "ModelName": "<the Azure Open AI model (such as gpt-4o) that the tests should use>",
  "Endpoint": "<the Azure Open AI endpoint url that the tests should use>",
  "StorageRootPath": "<the full path to a folder on your machine under which cached LLM responses and evaluation resuts for the tests should be stored>"
  }
```

The `appsettings.local.json` file is ignored via `.gitignore`, so it will not be committed when you make other changes
to the code in this repository.

You can also use the `ConfigureEvaluationTests.ps1` script available under the `scripts` folder at the root of this
repository to copy over previously created `appsettings.local.json` files to the current folder.
