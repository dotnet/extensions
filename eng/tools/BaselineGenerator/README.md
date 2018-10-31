BaselineGenerator
=================

This tool is used to generate an MSBuild file which sets the "baseline" against which servicing updates are built.

## Usage

1. Add to the [baseline.xml](./baseline.xml) a list of package ID's and their latest released versions.
2. Run `dotnet run` on this project.
