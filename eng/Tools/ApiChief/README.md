# About This Project

ApiChief is designed to help with API management activities. It provides five features:

* Prints a human-friendly summary of the public API of an assembly.

* Outputs a YAML file representing a fingerprint of the public API of an assembly.

* Outputs a YAML file representing a delta between a previously captured fingerprint and a current assembly.

* Fails if breaking changes are detected in an assembly relative to a previously captured fingerprint.

* Outputs API review files, needed to perform API reviews.

## Summary

You can output a summary of the public API of an assembly with:

```
ApiChief MyAssembly.dll emit summary
```

Use the -o option to specify a file where the output should be stored.

## Baseline

You can output a YAML file that represents a fingerprint of the public API of an assembly using:

```
ApiChief MyAssembly.dll emit baseline
```

Use the -o option to specify a file where the baseline should be stored.

## Delta

You can output a YAML file that captures the delta between a previously-captured fingerprint and an assembly:

```
ApiChief MyAssembly.dll delta MyPreviousBaseline.yml
```

Use the -o option to specify a file where the delta information should be stored.

## Breaking Changes

You can cause the command to return a failure code (useful from scripts) whenever an assembly's API
contains breaking changes relative to a previous API baseline fingerprint:

```
ApiChief MyAssembly.dll breaking MyPreviousBaseline.yml
```

## API Reviews

You can output a folder containing files that capture the public API surface of an
assembly, in a form suitable for performing API reviews.

```
ApiChief MyAssembly.dll emit review
```
