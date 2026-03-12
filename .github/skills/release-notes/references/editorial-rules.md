# Editorial Rules

## Tone

- Maintain a **positive tone** — highlight new benefits rather than expressing prior shortcomings
  - ✅ `Added streaming metrics for time-to-first-chunk and time-per-output-chunk`
  - ❌ `Previously there was no way to measure streaming latency`
- When context about the prior state is needed, keep it brief — one clause, not a paragraph — then pivot to the new capability

## Conciseness

- **No code samples** in release notes. This repository ships many packages and the release notes should be scannable, not tutorial-length.
- Each entry is a **single bullet point** with a brief description of what changed and why it matters.
- For complex features, use at most 2–3 sentences. Link to the PR for details.
- If a PR touches multiple concerns, describe the primary change. Do not enumerate every modified file.

## Entry format

Use this format (GitHub auto-links `#PR` and `@user` in release notes):

```
* Description #PR by @author
```

For PRs with co-authors (harvested from `Co-authored-by` commit trailers):
```
* Description #PR by @author (co-authored by @user1 @user2)
```

For Dependabot PRs, omit the author:
```
* Bump actions/checkout from 5.0.0 to 6.0.0 #1234
```

For Copilot-authored PRs, check the `copilot_work_started` timeline event to identify the triggering user. That person becomes the primary author; `@Copilot` becomes a co-author:
```
* Add trace-level logging for HTTP requests #1234 by @author (co-authored by @Copilot)
```

## Entry naming

- Prefer a **brief description** of what changed over simply stating an API name
  - ✅ `Added streaming metrics for time-to-first-chunk`
  - ✅ `Fixed credential handling in authenticated proxy scenarios`
  - ❌ `OpenTelemetryChatClient update`
  - ❌ `HttpClientHandler fix`
- Start with a verb: Added, Fixed, Updated, Removed, Improved, Renamed
- Keep entries to one line when possible

## Attribution rules

- **PR author**: The `user.login` from the PR details
- **Co-authors**: Harvest from `Co-authored-by` trailers in the PR's merge commit
- **Copilot**: Check the `copilot_work_started` timeline event. If present, the triggering user is the primary author and `@Copilot` is a co-author
- **Bots to exclude**: `dependabot[bot]`, `dotnet-maestro[bot]`, `github-actions[bot]`, `copilot-swe-agent[bot]`, and any account ending with `[bot]`

## Sorting

Within the **What's Changed** area sections, sort entries by **impact** (see [categorize-entries.md](categorize-entries.md) for the impact tier ordering). Within all other sections (Documentation Updates, Test Improvements, Repository Infrastructure Updates), sort entries by **merge date** (chronological order, oldest first).

## Category definitions

### What's Changed
Feature work, bug fixes, API improvements, performance enhancements, and any other user-facing changes. This includes:
- New API surface area
- Bug fixes that affect runtime behavior
- Performance improvements
- Changes that span code + docs (categorize by primary intent)

### Documentation Updates
PRs whose **sole purpose** is documentation:
- Fixing typos in docs
- Updating XML doc comments (when not part of a functional change)
- README updates

A PR that changes code AND updates docs belongs in "What's Changed."

### Test Improvements
PRs focused on test quality or coverage:
- Adding new tests
- Fixing broken or flaky tests
- Test infrastructure improvements

### Repository Infrastructure Updates
PRs that maintain the development environment:
- Version bumps
- CI/CD workflow changes
- Dependency updates (Dependabot)
- Build system changes
- Copilot instructions and skill updates

PRs that touch test code should never be categorized as Infrastructure.

## Acknowledgements section

Include an acknowledgements section at the bottom of the release notes:

1. **New contributors** — people making their first contribution in this release
2. **Issue reporters** — community members whose reported issues were resolved in this release, citing the resolving PR
3. **PR reviewers** — single bullet listing all reviewers, sorted by review count (no count shown)

Format:
```
* @user made their first contribution in #PR
* @user submitted issue #1234 (resolved by #5678)
* @user1 @user2 @user3 reviewed pull requests
```

## Inclusion criteria

Include a feature/fix if:
- It gives users something new or something that works better
- It's a community-requested change (high reaction count on backing issue)
- It changes behavior users need to be aware of

Exclude:
- Internal refactoring with no user-facing change
- Test-only changes (these go in "Test Improvements")
- Build/infrastructure changes (these go in "Repository Infrastructure Updates")
