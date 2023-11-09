# About this Project

This tool is designed to manipulate diagnostic config files. These are YAML files which each
provide information for all diagnostic messages that can be produced by a given Roslyn analyzer.
The information is broken down into two parts:

* Full metadata for each diagnostic including a title, description, category, help URI, and a set of
custom tags. This metadata is extracted from the analyzer assemblies via this tool and can be automatically
refreshed as new analyzers are added or existing ones are modified.

* A tier value which includes the relative importance of the diagnostics. Tier numbers start at 1 and go up from there.
The lower a diagnostic's tier value, the more relatively important the diagnostic is.

* A variable set of named attributes. Each attribute indicates a severity for the diagnostic, along with
an optional comment. Attributes and their severities are maintained by the .NET Extensions team and are used to
describe how to handle a given diagnostic for a given type of source code. More on attributes below.

Here's an example diagnostic:

```yaml
Diagnostics:
  CA1001:
    Metadata:
      Category: Design
      Title: Types that own disposable fields should be disposable
      Description: A class declares and implements an instance field that is a System.IDisposable type, and the class does not implement IDisposable. A class that declares an IDisposable field indirectly owns an unmanaged resource and should implement the IDisposable interface.
      HelpLinkUri: https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1001
      CustomTags:
      - PortedFromFxCop
      - Telemetry
      - EnabledRuleInAggressiveMode
      DefaultSeverity: None
    Tier: 2
    Attributes:
      general:
        Severity: Warning
```

## What's the Point?

The purpose of the tool is ultimately to emit `.editorconfig` files that can
be put in source repositories to control diagnostic messages produced for the source code. Different
`.editorconfig` files can be produced for different kinds of source code (production code, test code,
benchmark code, etc.)

## Attributes

Attributes are used to control what is ultimately put into a generated `.editorconfig` file. At the time you
generate an `.editorconfig` file, you specify the set of attributes to evaluate. For each diagnostic, the
tool compares the requested attributes with what has been defined in the diagnostic config file.
The severity value selected is based on the highest severity of any matching attribute.

Although the set of attributes is arbitrary, the currently used set is:

* `general`. Generally useful for any code
* `production`. The severity to use for production-level code being analyzed.
* `test`. The severity to use for test-level code being analyzed.
* `performance`. The severity to use for performance-sensitive code being analyzed.
* `api`. The severity to use for code that exposes APIs.

### Severity Levels

Attributes indicate a level of severity. The available severities are:

* `Default` - whatever the analyzer specified
* `None` - the analysis isn't performed
* `Silent` - ignored when building on the command-line, shown in the lightbulb menu in Visual Studio
* `Suggestion` - ignored when building on the command-line, shown as a Message in the Visual Studio Error List window
* `Warning` - shown as a Warning in the Visual Studio Error List window
* `Error` - shown as an Error in the Visual Studio Error List window

Refer to [this page](https://docs.microsoft.com/visualstudio/code-quality/use-roslyn-analyzers) for more details on these severity levels.

## Using the Tool

The tool has a few different commands described below. All these commands require you to specify the name of a directory where the diagnostic configuration
YAML files are kept. You start out with an empty directory, which gets populated by the tool, and updated over time by
the tool or by a human. The `eng/Diags` folder is where all these configuration files are located in the code base.

### Extracting Analyzer Metadata

Use the following to extract diagnostic metadata from a Roslyn analyzer assembly:

```console
> DiagConfig <config-directory> analyzer merge <analyzers>...
```

You specify the name of the config directory, along with file system paths to the analyzer assemblies of interest. The tool will read the assembly
and then either create or update metadata for the analyzer within the given config directory.

Once you have the analyzer's metadata, you can start editing the YAML files in the config directory in order to
adjust the severity level of the diagnostics.

> Hint!
> Check `scripts/MergeAnalyzerMetadata.ps1` to see how it is used in the code base.

### Import Existing Settings From an Editor Config File

If you already have an `.editorconfig` file which contains analyzer settings, you can extract them and insert them into
the config directory state.

```console
> DiagConfig <config-directory> editorconfig merge <editor-config-file> <editor-config-family>
```

You specify the name of the config directory, the path to an existing `.editorconfig` file, and the family to
update with the settings from the config file.

### Producing an Editor Config File

You use the following to produce an `.editorconfig` file:

```console
> DiagConfig <config-directory> editorconfig save <editor-config-file> [<editor-config-attributes>...]
```

> Hint!
> Check `scripts/MakeEditorConfigs.ps1` to see how it is used in the code base.

You specify the name of the config directory, the path of the editor config file to produce, and the set
of severity attributes to extract. When many families are supplied, for any given diagnostic the highest severity of
any matching attribute is what will be written to the editor config file being produced.

### Producing an 'All-Off Diagnostics' Config File

When our customers want to adopt static analysis they cannot do it all at once in bigger codebases.
Adding the analysis NuGet turns on all analyzers by default for given assembly.
By creating `all of` config file they can turn on analyzers only per specific directory and adopt it gradually.

```console
> DiagConfig <config-directory> editorconfig save --all-off <editor-config-file> [<editor-config-attributes>...]
```

> Hint!
> Check `MakeEditorConfigs.ps1` to see how it is used in the source base.
