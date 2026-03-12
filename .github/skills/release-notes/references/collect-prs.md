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

Also fetch reviews for the acknowledgements section:

```
pull_request_read(
  method: "get_reviews",
  owner: "dotnet",
  repo: "extensions",
  pullNumber: <number>
)
```

Record each reviewer's username and the PR number. See [editorial-rules.md](editorial-rules.md) for exclusion and sorting rules.

## Discover linked issues

Parse the PR body for issue references:
- Closing keywords: `Fixes #1234`, `Closes #1234`, `Resolves #1234`
- Full URL links: `https://github.com/dotnet/extensions/issues/1234`
- Cross-repo references: `dotnet/extensions#1234`

For each discovered issue, fetch details with `issue_read` and insert into the `issues` table.

## Deduplicate against prior release

PRs merged into `main` may include changes that were already included in a prior release via a `release/` branch. The prior release branch may also contain PRs that were merged into it but never covered in that release's notes — those PRs must still be excluded from the current release notes because they shipped in the prior release's packages.

### Fetch release branches

Before deduplication, ensure the relevant release branches are available locally:

1. Identify which local git remote points to `dotnet/extensions` on GitHub (e.g. by checking `git remote -v` for a URL containing `dotnet/extensions`). Use that remote name in subsequent fetch commands.
2. Identify the prior release branch from the previous release tag (e.g. `v10.3.0` → `release/10.3`).
3. Fetch it: `git fetch <remote> release/10.3` (using the remote identified in step 1).
4. If the current release also has a release branch (e.g. `release/10.4`), fetch that too.

### Exclude PRs already shipped

For each candidate PR, check whether it was already included in any prior release — even if the prior release notes didn't mention it:

1. **Check against the prior release tag**: `git merge-base --is-ancestor <pr-merge-commit> <previous-tag>`. If the PR's merge commit is an ancestor of the previous release tag, it shipped in that release — exclude it.
2. **Check against the prior release branch HEAD**: The release branch may have advanced beyond the release tag (e.g. `release/10.3` may contain commits merged after `v10.3.0` was tagged but before `v10.3.1` or the branch was abandoned). Check: `git merge-base --is-ancestor <pr-merge-commit> <remote>/release/10.3`. If reachable, the PR was part of that release branch's content — exclude it.
3. **Check the prior release notes body**: Fetch the GitHub release for the previous tag and check if the PR number appears in the release notes body. This catches PRs that were explicitly covered.

> **Why this matters:** A PR can be merged into a `release/` branch, ship in that release's packages, but never appear in that release's notes (e.g. a late-breaking fix). When that PR is later merged into `main`, it appears in the date-range search for the next release. Without branch-aware deduplication, it would be incorrectly included in the new release notes.

This step is critical and must run before marking PRs as candidates.

## Exclusion filters

Before marking PRs as candidates, exclude:
- PRs labeled `backport`, `servicing`, or `NO-MERGE`
- PRs whose title starts with `[release/` or contains `backport`
- PRs that are purely automated version bumps (title matches `Update version to *` and only changes `Directory.Build.props` or version files)

Mark remaining PRs as candidates: `UPDATE prs SET is_candidate = 1 WHERE ...`

## Populate co-author data

For each candidate PR, collect co-authors from **all commits in the PR**, not just the merge commit:

1. **Fetch the PR's commits** using `list_commits` on the PR's head branch, or use `get_commit` for the merge commit SHA from the PR details.
2. **Parse `Co-authored-by:` trailers** from every commit message in the PR. These trailers follow the format: `Co-authored-by: Name <email>`. Extract the GitHub username from the email (e.g. `123456+username@users.noreply.github.com` → `username`) or match the name against known GitHub users.
3. **Also check the merge commit** message itself for `Co-authored-by:` trailers, as squash-merged PRs consolidate trailers there.
4. **Check the `copilot_work_started` timeline event** to identify Copilot-assisted PRs where a human delegated the work.

A common pattern in this repository is a human-authored PR with `Co-authored-by: Copilot <...>` trailers on individual commits — these must be detected to give Copilot co-author attribution. Store all discovered co-authors in the database for use during rendering.
