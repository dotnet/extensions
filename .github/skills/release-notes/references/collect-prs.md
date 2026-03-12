# Collect and Filter PRs

Gather all merged PRs between the previous release tag and the target for this release.

## Determine the commit range

1. **Previous release tag**: Use `gh release list --repo dotnet/extensions --exclude-drafts --limit 10` to find the most recent published release. If the user specifies a particular previous version, use that instead.
2. **Target**: The user provides a target (commit SHA, branch, or tag). If none is specified, use the `HEAD` of the default branch (`main`).
3. Verify both refs exist: `git rev-parse <previous-tag>` and `git rev-parse <target>`.

## Search for merged PRs

### Primary — GitHub MCP server

Use `search_pull_requests` to find PRs merged in the date range. Keep result sets small to avoid large responses being saved to temp files.

```
search_pull_requests(
  owner: "dotnet",
  repo: "extensions",
  query: "is:merged merged:<start-date>..<end-date>",
  perPage: 30
)
```

Page through results (incrementing `page`) until all PRs are collected. The `start-date` is the merge date of the previous release tag's PR (or the tag's commit date); the `end-date` is the target commit date.

If the date range yields many results, split by week or use label-scoped queries to keep individual searches small.

### Fallback — GitHub CLI

If the MCP server is unavailable:

```bash
gh pr list --repo dotnet/extensions --state merged \
  --search "merged:<start-date>..<end-date>" \
  --limit 500 --json number,title,labels,author,mergedAt,url
```

## Assign packages from file paths

For each PR, fetch the list of changed files:

```
pull_request_read(
  method: "get_files",
  owner: "dotnet",
  repo: "extensions",
  pullNumber: <number>
)
```

Extract package names from file paths matching `src/Libraries/{PackageName}/`. For each matched package, look up the area group from [package-areas.md](package-areas.md).

**Rules:**
- A PR may affect multiple packages across different areas — record all of them in the `pr_packages` table
- If a PR only touches `test/Libraries/{PackageName}/`, it still maps to that package's area (useful for the "Test Improvements" category)
- If a PR only touches `eng/`, `scripts/`, `.github/`, or root-level files, it has no package assignment — categorize as infrastructure
- If a PR only touches `docs/`, it has no package assignment — categorize as documentation

**Fallback for ambiguous PRs:**
If a PR has no `src/Libraries/` or `test/Libraries/` file changes but does have `area-*` labels, use those labels to infer the package area. Map `area-Microsoft.Extensions.AI` to the AI area group, etc.

## Store PR data

Insert each discovered PR into the `prs` SQL table. See [sql-storage.md](sql-storage.md) for the schema.

## Enrich PR details

For each PR, fetch the full body and metadata:

```
pull_request_read(
  method: "get",
  owner: "dotnet",
  repo: "extensions",
  pullNumber: <number>
)
```

Update the `body`, `reactions`, `author_association`, and `labels` columns. Multiple independent PR reads can be issued in parallel.

Also fetch comments to look for Copilot-generated summaries:

```
pull_request_read(
  method: "get_comments",
  owner: "dotnet",
  repo: "extensions",
  pullNumber: <number>
)
```

## Discover linked issues

Parse the PR body for issue references:
- Closing keywords: `Fixes #1234`, `Closes #1234`, `Resolves #1234`
- Full URL links: `https://github.com/dotnet/extensions/issues/1234`
- Cross-repo references: `dotnet/extensions#1234`

For each discovered issue, fetch details with `issue_read` and insert into the `issues` table.

## Exclusion filters

Before marking PRs as candidates, exclude:
- PRs labeled `backport`, `servicing`, or `NO-MERGE`
- PRs whose title starts with `[release/` or contains `backport`
- PRs that are purely automated version bumps (title matches `Update version to *` and only changes `Directory.Build.props` or version files)

Mark remaining PRs as candidates: `UPDATE prs SET is_candidate = 1 WHERE ...`

## Populate co-author data

For each candidate PR, check the merge commit for `Co-authored-by:` trailers. Record co-authors for use in attribution. Check the `copilot_work_started` timeline event to identify Copilot-assisted PRs.
