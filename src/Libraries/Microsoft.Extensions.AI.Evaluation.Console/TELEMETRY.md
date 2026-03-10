# Telemetry in the `aieval` .NET tool

## Overview

The `aieval` .NET tool (Microsoft.Extensions.AI.Evaluation.Console) collects anonymous usage data to help us understand how the tool is being used and to improve your experience. This document describes the telemetry data collected, how to opt out, and our commitment to your privacy.

## When is telemetry collected?

Telemetry is collected only when you use the **`aieval` command-line .NET tool** for operations such as:
- Generating evaluation reports (`dotnet aieval report`)
- Cleaning cached evaluation results (`dotnet aieval clean-cache`)
- Cleaning stored evaluation results (`dotnet aieval clean-results`)

Note that using the Microsoft.Extensions.AI.Evaluation libraries in your own code **does not** trigger any telemetry collection. Telemetry is only collected when explicitly using the `aieval` command-line .NET tool.

## What data is collected?

The tool collects anonymous usage information to help us understand how the tool and the evaluation libraries are being used, and to help us identify areas for improvement.

Below are some examples of the kinds of data collected:

### System and environment details
- Version of the `aieval` .NET tool
- .NET runtime version
- Operating system platform and version
- Whether the tool was running in a CI environment

### Command usage
- Which tool commands were executed (`report`, `clean-cache`, `clean-results`)
- Command execution status (success v/s failure)
- Command execution duration
- Whether the persisted evaluation data for report generation was stored on local disk v/s azure blob storage
- Report generation format (e.g., HTML, JSON)

### Evaluation functionality used
- Number of scenarios evaluated
- Whether or not the response caching functionality was utilized during evaluation
- Names of built-in evaluation metrics used (e.g., Coherence, Fluency, Groundedness) and whether any errors were encountered when computing these metrics
- Model used for evaluation and (input and output) token counts
- Evaluation duration

## What data is NOT collected?

We are committed to protecting your privacy. All telemetry collected by the `aieval` .NET tool is anonymous and does not contain any personally identifiable information. Additionally, the telemetry collected by the `aieval` .NET tool **does not** include any information that falls into the following categories:

- **Your prompts or conversation content** - The content of your LLM prompts, conversation histories, tool calls, and AI responses that you are evaluating
- **Contextual data passed to evaluators** - Any additional data you provide as input to the evaluation process
- **Custom evaluator implementation details** - Code or logic from your custom evaluators (including details pertaining to your own custom evaluation metrics)
- **File paths or names** - Local file system paths or file names from your projects
- **Any other personal or sensitive information** - Personal data, credentials, usernames, email addresses, URLs, or any other sensitive information

## How to opt out

You can opt out of telemetry collection by setting the `DOTNET_AIEVAL_TELEMETRY_OPTOUT` environment variable to `1` or `true`.

## First-run experience

The first time you run a particular version of the `aieval` .NET tool, you will see a message informing you about telemetry collection and how to opt out.

```
---------
Telemetry
---------
The aieval .NET tool collects usage data in order to help us improve your experience. The data is anonymous and doesn't include personal information. You can opt-out of this data collection by setting the DOTNET_AIEVAL_TELEMETRY_OPTOUT environment variable to '1' or 'true' using your favorite shell.
```

To skip the above first-run telemetry notification (for example, in automated environments), you can set the `DOTNET_AIEVAL_SKIP_FIRST_TIME_EXPERIENCE` environment variable to `1` or `true`. Note that this only suppresses the notification message; telemetry will still be collected unless you also set the `DOTNET_AIEVAL_TELEMETRY_OPTOUT` environment variable.

## Data handling and privacy

- All telemetry data collected is anonymous and does not contain personally identifiable information.
- The data is used to improve the Microsoft.Extensions.AI.Evaluation libraries and the `aieval` .NET tool.
- The data is sent securely to Microsoft servers and managed in accordance with Microsoft's privacy policies.
