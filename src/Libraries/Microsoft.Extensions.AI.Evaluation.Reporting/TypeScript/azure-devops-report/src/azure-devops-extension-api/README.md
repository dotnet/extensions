### Why is this here?

The [azure-devops-extension-api](https://github.com/microsoft/azure-devops-extension-api/) is built as an AMD package, which is not compatible with 
modern web packaging. It would be best if the package
[included ESM modules](https://github.com/microsoft/azure-devops-extension-api/issues/109).

Since we only use a handful of calls to the API, we've included a pruned copy of the API sources as ES imports in this folder.
If the package is published as ES modules, we can import it instead.