# AI PROJECT TEMPLATE VALIDATION

You are an AI assistant whose job is to test and validate variations of an AI Chat Web .NET project template.

**TL;DR**
For each `<test ai_status="pending">` in `AI_TODO.txt`: run the test project, open the printed app URL, send **exactly** `What's in the kit?` to the web chat, apply three validation checks (search logged, survival-kit content, citations), save screenshot under `AI-OUTPUT/`, set `ai_status` to `passed|failed|skipped`, and write a short `<ai_remarks>` entry explaining what you did and why.

---

## QUICK REFERENCES

* `AI_TODO.txt` path:
  `./test/ProjectTemplates/Microsoft.Extensions.AI.Templates.IntegrationTests/ExecutionTestSandbox/AI_TODO.txt`
* Evidence dir:
  `./test/ProjectTemplates/Microsoft.Extensions.AI.Templates.IntegrationTests/ExecutionTestSandbox/AI-OUTPUT/`

---

## CONSTITUTION

* Start by creating an individual TODO for each pending test in `AI_TODO.txt`
* Run each test as its own subagent
* Don't run any commands other than variations of the ones listed below:
   * `./test/ProjectTemplates/Microsoft.Extensions.AI.Templates.IntegrationTests/ExecutionTestSandbox/Start-Project.ps1`
   * `./test/ProjectTemplates/Microsoft.Extensions.AI.Templates.IntegrationTests/ExecutionTestSandbox/Stop-Project.ps1`
   * `Start-Sleep`
     * Wait a max of 3 seconds for most operations
     * For known long-running operations (like provisioning Azure resources), wait at most 30 seconds at a time
   * `Get-Content`
   * `Copy-Item`

Using the Playwright tools when interacting with the browser. If these aren't available, end the testing procedure.

Aspire projects require configuration via the Aspire dashboard. Please provide this configuration when prompted:
* Azure Subscription ID: Read from the `AZURE_SUBSCRIPTION_ID` environment variable
* Location: East US 2

If you are unable to do what you need without using only these commands, consider the test failed. In the `<ai_remarks>`, describe how you got stuck.

DO NOT EVER preemptively skip a project, even if you think it's destined to fail. It's important to run all projects so that we get detailed error reports, even if the error is caused by missing configuration.

## PER-TEST TASKS

Follow these steps for one test. Keep outputs short and machine-parseable where useful.

1. **Find next test**

   * Open `AI_TODO.txt` and locate the *first* `<test ai_status="pending">` block. Use its `<args>` and `<path>` values.

2. **Pre-run checks**

   * Open the project's `README.md` (in the `<path>` folder) to learn how the project works. Assume pre-run configuration values have already been set.

3. **Run the project**
   * From the repo root, run the `Start-Project.ps1` script, passing the directory specified in `<path>`:

     ```powershell
     .\test\ProjectTemplates\Microsoft.Extensions.AI.Templates.IntegrationTests\ExecutionTestSandbox\Start-Project.ps1 -ProjectPath "<path-from-AI_TODO>"
     ```
   * If the project fails due to missing configuration, skip the project and set `ai_status="skipped"`.
   * Before moving to the next project, always terminate the already-running one using `Stop-Project -ProcessId <pid_from_start_project>`

4. **Detect the app URL**

   * The runner will output a file path containing the application's standard output.
   * Read this file to obtain the launch URL.
   * If app is still starting, try again.
   * NOTE: The page may fail to load at first while the app initializes. Retry up to 3 times in 30 second intervals. If the page still fails to load, consider the test failed.

5. **Open UI & send the test query**

   * Open a browser tab to the app URL. If the app shows a web chat, focus the chat text box and send **exactly**:
     ```
     What's in the kit?
     ```
   * Wait up to 60 seconds for a reply. If no reply, capture a screenshot and mark the run as `failed`.

6. **Validation checks** (all three must pass to mark `passed`)

   * **Search logged** — the app must show at least one chat entry indicating a search occurred.
   * **Survival kit content** — the reply must reference a survival kit.
   * **Citations present** — the reply must include citations at the end of its message.

7. **Decide status**

   * If all three checks pass → `ai_status="passed"`.
   * If the app responded but one or more checks failed → `ai_status="failed"`.
   * If configuration was missing before run → `ai_status="skipped"`.
   * If the app never started or had runtime errors → `ai_status="failed"` (include error excerpts in remarks).

8. **Produce evidence**

   * Save a screenshot: `<test-id>_<status>_screenshot.png` into `.../AI-OUTPUT/`.

9. **Update `AI_TODO.txt`**

   * In the same `<test ...>` block, change the `ai_status` attribute value to `passed`, `failed`, or `skipped`.
   * Add a concise `<ai_remarks>` entry (replace the empty element content). Preserve the existing test block structure and the closing `<test/>` token used in the file.

10. **Stop the app**

  * Stop the app using `Stop-Project -ProcessId <pid_from_start_project>`
  * Close the browser window

---

## What to put in `<ai_remarks>`

Include at least:
* which checks passed/failed,
* paths to screenshots,
* one-line note/reason (if failed, include the error or missing config).

**Example**:

```xml
<ai_remarks>
   screenshot: AI-OUTPUT/AIChatWeb_8512ad7cfe_screenshot_passed.png
   note: Response included survival kit details and citations.
</ai_remarks>
```
