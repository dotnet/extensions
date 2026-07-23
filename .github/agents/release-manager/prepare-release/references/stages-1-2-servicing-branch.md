# Servicing Release Preparation (Patch Releases)

Use this flow for patch releases (`<major>.<minor>.<patch>`) prepared directly on the latest public `release/<major>.<minor>` branch.

This is a **different track** from Stage 1/2 internal-branch prep. Do not mix them unless the user explicitly overrides.

## Outcome

Produce a servicing-prep PR into `release/<major>.<minor>` that includes:

1. Patch-version bump commit.
2. Cherry-picks of user-selected `main` commits (in `main` merge order).
3. Any required release-branch-specific touch-ups.

Then stop for merge/approval before publish steps.

## Step 1: Confirm target branch and patch version

1. Confirm the target public branch (`release/<major>.<minor>`).
2. Confirm the target patch version (`<major>.<minor>.<patch>`).
3. Create a working branch from `origin/release/<major>.<minor>`.

## Step 2: Build the candidate backport list from `main`

The user needs a clear **selection list** of commits from `main` that are not yet included in `release/<major>.<minor>`.

For grounding, also present a separate view of what is already merged into `release/<major>.<minor>` (whether those merges happened before or after the target patch-version bump point).

### 2a) Gather candidate PRs/commits

Build candidates from commits in `main` that are not in `release/<major>.<minor>`, then map them to PR numbers where possible.

### 2b) Build grounding for already-merged release-branch items

Identify items that already shipped on the release branch by checking merged PRs into `release/<major>.<minor>` and harvesting referenced `#NNNN` slugs from their titles/bodies.

Present these as **grounding only** (not selectable candidates). If a patch-bump commit for the target patch exists on the release branch, label each grounding row as:

- `before patch bump`, or
- `after patch bump`

If no patch-bump commit exists yet on the release branch, label rows as `before planned patch bump`.

### 2c) Enrich each candidate with affected libraries

For each candidate PR:

1. Read changed files.
2. Derive affected libraries from touched paths (for example, `src/Libraries/<LibraryName>/...`).
3. Keep the list unique and sorted.

### 2d) Present the selection table

Show a selection table with one row per **selectable** candidate item:

| SHA (short) | Title | PR | Libraries affected |
|---|---|---|---|
| `abc1234` | Commit/PR title | `#1234` link | `Microsoft.Extensions.AI`, ... |

Then show a separate grounding table for already-merged release-branch items:

| Timing vs patch bump | SHA (short) | Title | PR | Libraries affected |
|---|---|---|---|---|
| before patch bump / after patch bump / before planned patch bump | `def5678` | Commit/PR title | `#5678` link | `Microsoft.Extensions.AI.Abstractions`, ... |

Requirements:

- Keep already-merged release-branch rows out of the selectable table.
- Prefer PR-number rows when available.
- If a commit has no PR number, mark it and ask the user whether to include it explicitly.

## Step 3: Ask the user to choose items (multi-select)

Prompt the user to select rows from the **selectable candidate table only** (for example by PR numbers or row numbers, comma-separated).

Do not cherry-pick until selection is explicit.

## Step 4: Confirm package scope and template inclusion

Before committing:

1. Propose the package scope derived from selected items (default to coherent related package sets). For example, when any of Microsoft.Extensions.AI, Microsoft.Extensions.AI.Abstractions, and Microsoft.Extensions.AI.OpenAI are updated, all three are released, but the Microsoft.Extensions.AI.Evaluation packages are not released unless they are also updated.
2. Ask the user to confirm or adjust that package scope.
3. If selected changes affect packages used by project templates, ask whether template packages should also be included in the servicing release.

Record this confirmed scope; it is the source of truth for publish/validate/release-notes stages.

## Step 5: Apply commits in servicing order

Apply commits in this exact order:

1. **Bump patch version first**.
2. **Cherry-pick selected commits** in the order they merged into `main`.
3. **Apply touch-ups** needed only for the servicing release branch.

For each cherry-pick, record whether it was:

- **Clean cherry-pick** (no merge conflicts), or
- **Cherry-pick with merge conflicts** (include conflict-resolution notes).

### Patch version bump details

- Update `eng/Versions.props` patch version for the target release.
- Commit message format: `Bump version to <major>.<minor>.<patch>`.
- This matches recent 10.* servicing examples (`10.4.1`, `10.5.1`, `10.5.2`, `10.8.1`).

## Step 6: Create the servicing-prep PR

Do not push or open the PR until the user explicitly says to do so.

When creating the PR into `release/<major>.<minor>`, apply the `DO-NOT-SQUASH` label at creation time.

PR title format:

`Prepare <major>.<minor>.<patch> Servicing Release`

PR body format:

```md
Prepares the <major>.<minor>.<patch> servicing release for the following packages:
- <package-name> (<version-if-different-or-prerelease>)
- <package-name> (<version-if-different-or-prerelease>)
- <package-name> (<version-if-different-or-prerelease>)

## Commits included
- Bump version to <major>.<minor>.<patch>
- Clean cherry-pick #<pr-number>
- Cherry-pick with merge conflicts #<pr-number>
  - Conflict: <file-or-area>; Resolution: <how it was resolved>
  - Conflict: <file-or-area>; Resolution: <how it was resolved>
```

Notes:

- Use `#<pr-number>` slugs for included PRs; GitHub renders number/title automatically.
- Keep commit order in the PR aligned with Step 5.
- For every conflicted cherry-pick, include one or more indented sub-bullets describing the conflict(s) and resolution(s).
- If a selected commit has no PR number, use its short SHA in place of `#<pr-number>`.
- Keep the `DO-NOT-SQUASH` label on the PR from creation through merge.
- For the merge method into `release/<major>.<minor>`, prefer **Rebase and merge** (never squash).

## Step 7: Preserve scope for downstream stages

After PR creation, treat the merged servicing-prep PR description as authoritative for:

- package publish scope (publish-release),
- package validation scope (validate-release),
- package scope in release notes (write-release-notes).

If scope changes later, update the PR description and confirm with the user before publishing.

## After this preparation

After the servicing-prep PR merges into `release/<major>.<minor>`, continue with **publish-release** servicing flow:

1. wait for mirror into AzDO,
2. run `extensions-ci-official` from `release/<major>.<minor>`,
3. publish selected packages,
4. continue post-release checks and notes.
