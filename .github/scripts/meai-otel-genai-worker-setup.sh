#!/usr/bin/env bash
# Host-side setup for the "MEAI: Otel GenAI Worker" agentic workflow.
#
# Deterministically prepares everything the agent needs before it runs so the agent
# never has to discover, filter, or de-duplicate state itself:
#   1. Resolves the upstream scan target (from the orchestrator's `target` input, or from a
#      standalone dispatch's upstream_ref / default HEAD), retrying transient failures and
#      hard-failing when the ref cannot be resolved to a commit SHA at all.
#   2. Discovers the maintained draft PR and classifies it: OURS (carries our tracking
#      marker), ADOPT (a human-bootstrapped automation+area-ai PR on our branch with no
#      marker yet), BLOCKED (a non-automation PR occupying our branch -- a human owns it),
#      or NONE.
#   3. Reads the PR's recorded Upstream-Scan-Ref and Upstream-Release, and computes a
#      recommended lifecycle action.
#
# Writes (under $AGENT_DIR, uploaded in the agent artifact so downstream jobs read them):
#   target.json         resolved scan target + PR discovery + recommended action
#
# The recommended action lets a caught-up run early-noop before the expensive build. The
# agent's Step 3 remains authoritative and refines edge cases (e.g. release-gate mark-ready).
#
# A transient API blip must never masquerade as "no PR / no work"; discovery is retried
# and, when the maintained PR cannot be established with confidence, the action defaults
# to "produce" (never a silent skip).
#
# Environment:
#   GITHUB_REPOSITORY  owner/repo (set by Actions)
#   GH_TOKEN           token with pull-requests:read (the agent job's github.token)
#   TARGET_JSON        target object from the orchestrator (workflow_call); may be empty
#   UPSTREAM_REF       standalone-dispatch scan ref override (used only when TARGET_JSON empty)
#   AGENT_DIR          output dir (default /tmp/gh-aw/agent)
set -euo pipefail

AGENT_DIR="${AGENT_DIR:-/tmp/gh-aw/agent}"
REPO="${GITHUB_REPOSITORY:-}"
TARGET_JSON="${TARGET_JSON:-}"
UPSTREAM_REF="${UPSTREAM_REF:-}"
mkdir -p "$AGENT_DIR"
target_file="$AGENT_DIR/target.json"

run_started_at="$(date -u +%Y-%m-%dT%H:%M:%SZ)"

# ---- 1. Resolve the upstream scan target -------------------------------------------
UPSTREAM_REPO="open-telemetry/semantic-conventions-genai"
DESIRED_BRANCH="update-otel-genai-to-latest"
# The maintained PR always targets main (see the worker's create-pull-request
# `base-branch: main` safe output). PR discovery restricts to this base so a tracking-branch
# PR opened against some other base is never mistaken for the maintained PR.
BASE_BRANCH="main"
MARKER_ID="otel-genai"
upstream_sha=""
if [ -n "$TARGET_JSON" ]; then
	UPSTREAM_REPO="$(jq -r '.upstream_repo // "open-telemetry/semantic-conventions-genai"' <<<"$TARGET_JSON")"
	DESIRED_BRANCH="$(jq -r '.desired_branch // "update-otel-genai-to-latest"' <<<"$TARGET_JSON")"
	MARKER_ID="$(jq -r '.marker_id // "otel-genai"' <<<"$TARGET_JSON")"
	UPSTREAM_REF="$(jq -r '.upstream_ref // ""' <<<"$TARGET_JSON")"
	upstream_sha="$(jq -r '.upstream_sha // ""' <<<"$TARGET_JSON")"
fi

resolve_sha() {
	local ref="$1" sha="" attempt db
	for attempt in 1 2 3; do
		if [ -z "$ref" ]; then
			db="$(gh api "repos/${UPSTREAM_REPO}" -q '.default_branch' 2>/dev/null || true)"
			[ -n "$db" ] && sha="$(gh api "repos/${UPSTREAM_REPO}/commits/${db}" -q '.sha' 2>/dev/null || true)"
		else
			sha="$(gh api "repos/${UPSTREAM_REPO}/commits/${ref}" -q '.sha' 2>/dev/null || true)"
		fi
		[ -n "$sha" ] && { printf '%s' "$sha"; return 0; }
		[ "$attempt" -lt 3 ] && sleep 2
	done
	return 0
}

resolve_release_commit() {
	local tag="$1" ref_json="" object_type="" object_sha="" tag_json=""
	[ -z "$tag" ] && return 0

	ref_json="$(gh api "repos/${UPSTREAM_REPO}/git/ref/tags/${tag}" 2>/dev/null || true)"
	object_type="$(jq -r '.object.type // empty' <<<"$ref_json" 2>/dev/null || true)"
	object_sha="$(jq -r '.object.sha // empty' <<<"$ref_json" 2>/dev/null || true)"
	case "$object_type" in
		commit)
			[ -n "$object_sha" ] && { printf '%s' "$object_sha"; return 0; } ;;
		tag)
			tag_json="$(gh api "repos/${UPSTREAM_REPO}/git/tags/${object_sha}" 2>/dev/null || true)"
			object_type="$(jq -r '.object.type // empty' <<<"$tag_json" 2>/dev/null || true)"
			object_sha="$(jq -r '.object.sha // empty' <<<"$tag_json" 2>/dev/null || true)"
			[ "$object_type" = "commit" ] && [ -n "$object_sha" ] && { printf '%s' "$object_sha"; return 0; } ;;
	esac

	# Fallback for lightweight tags or unusual ref layouts; the commits endpoint
	# resolves branch/tag names to the commit GitHub would show for that ref.
	object_sha="$(gh api "repos/${UPSTREAM_REPO}/commits/${tag}" -q '.sha' 2>/dev/null || true)"
	[ -n "$object_sha" ] && printf '%s' "$object_sha"
	# Always succeed: the result is communicated via stdout (the caller tests for a
	# non-empty commit), so an unresolved tag must degrade to low confidence -- never
	# abort the run under `set -e` when this test is the function's final command.
	return 0
}

if [ -z "$upstream_sha" ] && [ -n "${GH_TOKEN:-}" ]; then
	upstream_sha="$(resolve_sha "$UPSTREAM_REF")"
	if [ -z "$upstream_sha" ]; then
		echo "::error::Could not resolve upstream ref '${UPSTREAM_REF:-<default branch HEAD>}' in ${UPSTREAM_REPO} to a commit SHA after retries. Refusing to continue with an unresolved scan target."
		exit 1
	fi
fi

# Best-effort latest published gen-ai release tag (normally none -- Development stability).
upstream_release="none"
upstream_release_commit=""
release_matches_scan="false"
release_matches_pr="false"
release_ready="false"
release_detection_confidence="none"
release_detection_signal="No GitHub release is published for the upstream repo."
if [ -n "${GH_TOKEN:-}" ]; then
	rel="$(gh api "repos/${UPSTREAM_REPO}/releases/latest" -q '.tag_name' 2>/dev/null || true)"
	if [ -n "$rel" ]; then
		upstream_release="$rel"
		upstream_release_commit="$(resolve_release_commit "$rel")"
		if [ -n "$upstream_release_commit" ]; then
			release_detection_confidence="high"
			release_detection_signal="GitHub latest release ${rel} resolves through refs/tags to commit ${upstream_release_commit}."
			[ -n "$upstream_sha" ] && [ "$upstream_release_commit" = "$upstream_sha" ] && release_matches_scan="true"
		else
			release_detection_confidence="low"
			release_detection_signal="GitHub latest release ${rel} exists, but its tag commit could not be resolved; release-gate readiness is intentionally withheld."
		fi
	fi
fi

write_target() {
	# $1=pr $2=pr_state $3=pr_is_draft $4=pr_recorded_sha $5=pr_recorded_release
	# $6=classification(ours|adopt|blocked|none) $7=action
	jq -cn \
		--arg upstream_repo "$UPSTREAM_REPO" --arg upstream_ref "$UPSTREAM_REF" \
		--arg upstream_sha "$upstream_sha" --arg upstream_release "$upstream_release" \
		--arg upstream_release_commit "$upstream_release_commit" \
		--argjson release_matches_scan "$release_matches_scan" \
		--argjson release_matches_pr "$release_matches_pr" \
		--argjson release_ready "$release_ready" \
		--arg release_detection_confidence "$release_detection_confidence" \
		--arg release_detection_signal "$release_detection_signal" \
		--arg desired_branch "$DESIRED_BRANCH" --arg pr_branch "${PR_BRANCH:-}" \
		--arg marker_id "$MARKER_ID" \
		--arg pr "$1" --arg pr_state "$2" --argjson pr_is_draft "${3:-false}" \
		--arg pr_recorded_sha "$4" --arg pr_recorded_release "$5" \
		--arg classification "$6" --arg action "$7" \
		--arg run_started_at "$run_started_at" \
		'{upstream_repo:$upstream_repo, upstream_ref:$upstream_ref, upstream_sha:$upstream_sha,
		  upstream_release:$upstream_release, upstream_release_commit:$upstream_release_commit,
		  release_matches_scan:$release_matches_scan, release_matches_pr:$release_matches_pr,
		  release_ready:$release_ready,
		  release_detection_confidence:$release_detection_confidence,
		  release_detection_signal:$release_detection_signal,
		  desired_branch:$desired_branch, pr_branch:$pr_branch, marker_id:$marker_id,
		  pr:$pr, pr_state:$pr_state, pr_is_draft:$pr_is_draft, pr_recorded_sha:$pr_recorded_sha,
		  pr_recorded_release:$pr_recorded_release, classification:$classification, action:$action,
		  run_started_at:$run_started_at}' >"$target_file"
}

step_summary() {
	# $1=classification $2=action $3=pr $4=pr_recorded_sha
	{
		echo "## Otel GenAI worker -- setup decision"
		echo ""
		echo "| field | value |"
		echo "|---|---|"
		echo "| upstream_repo | \`${UPSTREAM_REPO}\` |"
		echo "| scan ref | \`${UPSTREAM_REF:-<default HEAD>}\` |"
		echo "| upstream SHA | \`${upstream_sha:-<unresolved>}\` |"
		echo "| upstream release | \`${upstream_release}\` |"
		echo "| upstream release commit | \`${upstream_release_commit:-<unresolved>}\` |"
		echo "| release detection confidence | \`${release_detection_confidence}\` |"
		echo "| release matches scanned SHA | ${release_matches_scan} |"
		echo "| release matches PR SHA | ${release_matches_pr} |"
		echo "| release ready | ${release_ready} |"
		echo "| maintained PR | ${3:-<none>} |"
		echo "| maintained PR branch | \`${PR_BRANCH:-<none>}\` |"
		echo "| PR recorded SHA | \`${4:-<none>}\` |"
		echo "| classification | **${1}** |"
		echo "| recommended action | **${2}** |"
		[ "${tracking_valid_count:-0}" -gt 1 ] && echo "| valid tracking PR candidates | ${tracking_valid_count} (newest selected) |"
	} >>"${GITHUB_STEP_SUMMARY:-/dev/null}" 2>/dev/null || true
}

fetch_pr_body() {
	local pr_number="$1" battempt
	body="" body_ok="false"
	for battempt in 1 2 3; do
		if body="$(gh pr view "$pr_number" --repo "$REPO" --json body -q '.body' 2>/dev/null)"; then
			body_ok="true"; return 0
		fi
		[ "$battempt" -lt 3 ] && sleep 2
	done
	return 0
}

tracking_value() {
	local name="$1"
	# Tolerate an optional leading markdown list/quote marker (-, *, +, >) before the field
	# name. The identity guard that gates a PR write accepts the field in this same anchored,
	# tolerant form, so anything it admits is recoverable here -- otherwise a stray bullet
	# would extract to an empty value and wedge the state machine into a permanent
	# produce/re-wake loop (an empty recorded SHA never matches; an empty watermark treats
	# every comment as new).
	sed -n "s/^[[:space:]]*[-*+>]\{0,1\}[[:space:]]*${name}:[[:space:]]*//p" \
		| head -1 | tr -d $'"\'\r' | sed 's/[[:space:]]*#.*$//; s/[[:space:]]*$//'
}

if [ -z "$REPO" ] || [ -z "${GH_TOKEN:-}" ]; then
	echo "GITHUB_REPOSITORY or GH_TOKEN unset; cannot discover PR -- defaulting to produce (fresh)"
	write_target "" "" false "" "" "none" "produce"
	step_summary "none" "produce" "" ""
	exit 0
fi

# ---- 2. Discover the maintained PR and classify it ---------------------------------
# Stage 1: query the canonical branch server-side by exact head. This avoids the
# default `gh pr list` first-30 limit and preserves the invariant that a human-owned PR
# on the canonical branch blocks the automation.
BASE_OWNER="${REPO%%/*}"
pr="" PR_BRANCH="" pr_labels="" pr_is_draft="false" classification="none" body_ok="false" tracking_valid_count=0
for attempt in 1 2 3; do
	rows="$(gh api --method GET "repos/${REPO}/pulls" \
		-f state=open -f head="${BASE_OWNER}:${DESIRED_BRANCH}" -f base="${BASE_BRANCH}" -f per_page=100 2>/dev/null \
		| jq -c --arg owner "$BASE_OWNER" --arg branch "$DESIRED_BRANCH" --arg base "$BASE_BRANCH" \
			'[.[] | select((.head.repo.owner.login // "") == $owner and .head.ref == $branch and (.base.ref // "") == $base)
			  | {number, headRefName:.head.ref, headRepositoryOwner:{login:(.head.repo.owner.login // "")},
			     isDraft:(.draft // false), labels:(.labels // []), updatedAt:.updated_at}]
			 | sort_by(.updatedAt) | reverse' 2>/dev/null || true)"
	[ -n "$rows" ] && break
	[ "$attempt" -lt 3 ] && sleep 2
done
rows="${rows:-[]}"

pr="$(jq -r '(.[0].number) // empty' <<<"$rows" 2>/dev/null || true)"
if [ -n "$pr" ]; then
	pr_is_draft="$(jq -r '.[0].isDraft // false' <<<"$rows")"
	PR_BRANCH="$(jq -r '.[0].headRefName // ""' <<<"$rows")"
	has_automation="$(jq -r '[.[0].labels[]?.name] | (index("automation") != null) and (index("area-ai") != null)' <<<"$rows")"
	# Fetch the PR body with retries. A transient blip must not drop our tracking
	# marker -- that would misclassify our own PR as a fresh adopt.
	fetch_pr_body "$pr"
	has_marker="false"
	printf '%s' "$body" | grep -q "${MARKER_ID}-tracking:begin" && has_marker="true"
	if [ "$body_ok" != "true" ] && [ "$has_automation" = "true" ]; then
		# Body unreadable after retries but our automation labels are present: almost
		# certainly our own PR mid-blip. Treat as ours so the agent can re-read and reconcile.
		classification="ours"
	elif [ "$has_automation" = "true" ] && [ "$has_marker" = "true" ]; then
		classification="ours"
	elif [ "$has_automation" = "true" ] && [ "$has_marker" != "true" ]; then
		# Automation-labeled PR on our branch with no tracking marker yet: a human
		# bootstrapped it -- adopt it (write the marker on this run's update).
		classification="adopt"
	else
		# A PR occupies our branch but is not our automation PR: a human owns it. Stand down.
		classification="blocked"
	fi
else
	# Stage 2: no canonical PR exists. Look for a prior automation tracking PR on the
	# canonical branch or on a run-suffixed branch (`desired_branch_<run_id>`). Keep this
	# lightweight and fetch each candidate body individually so open PR history cannot
	# grow into a truncated bulk-body parse.
	for attempt in 1 2 3; do
		rows="$(gh pr list --repo "$REPO" --state open --base "$BASE_BRANCH" --label automation --label area-ai --limit 1000 \
			--json number,headRefName,headRepositoryOwner,isDraft,labels,updatedAt,baseRefName 2>/dev/null \
			| jq -c --arg owner "$BASE_OWNER" --arg branch "$DESIRED_BRANCH" --arg base "$BASE_BRANCH" \
				'map(select((.headRepositoryOwner.login // "") == $owner
				  and (.baseRefName // "") == $base
				  and (.headRefName == $branch
				    or ((.headRefName | startswith($branch + "_"))
				      and ((.headRefName | ltrimstr($branch + "_")) | test("^[0-9]+$"))))))
				 | sort_by(.updatedAt) | reverse' 2>/dev/null || true)"
		[ -n "$rows" ] && break
		[ "$attempt" -lt 3 ] && sleep 2
	done
	rows="${rows:-[]}"
	row_count="$(jq -r 'length' <<<"$rows" 2>/dev/null || echo 0)"
	selected_body=""
	for i in $(seq 0 $((row_count - 1)) 2>/dev/null); do
		candidate_pr="$(jq -r ".[$i].number // empty" <<<"$rows")"
		[ -n "$candidate_pr" ] || continue
		fetch_pr_body "$candidate_pr"
		[ "$body_ok" = "true" ] || continue
		printf '%s' "$body" | grep -q "${MARKER_ID}-tracking:begin" || continue
		candidate_sha="$(printf '%s\n' "$body" | tracking_value "Upstream-Scan-Ref")"
		printf '%s' "$candidate_sha" | grep -Eq '^[0-9a-fA-F]{7,}$' || continue
		tracking_valid_count=$((tracking_valid_count + 1))
		if [ -z "$pr" ]; then
			pr="$candidate_pr"
			pr_is_draft="$(jq -r ".[$i].isDraft // false" <<<"$rows")"
			PR_BRANCH="$(jq -r ".[$i].headRefName // \"\"" <<<"$rows")"
			classification="ours"
			selected_body="$body"
		fi
	done
	if [ -n "$pr" ]; then
		body="$selected_body"
		body_ok="true"
	fi
fi

# ---- 3. Read recorded state + compute the recommended action -----------------------
pr_recorded_sha="" pr_recorded_release=""
if [ -n "$pr" ] && [ "$classification" != "blocked" ] && [ "$body_ok" = "true" ]; then
	pr_recorded_sha="$(printf '%s\n' "$body" | tracking_value "Upstream-Scan-Ref")"
	pr_recorded_release="$(printf '%s\n' "$body" | tracking_value "Upstream-Release")"
fi
[ -n "$upstream_release_commit" ] && [ -n "$pr_recorded_sha" ] && [ "$upstream_release_commit" = "$pr_recorded_sha" ] && release_matches_pr="true"
[ "$release_matches_pr" = "true" ] && release_ready="true"

# ---- 4. Recommended lifecycle action ------------------------------------------------
# Only a high-confidence caught-up state early-noops; every uncertain case produces so
# the agent can make the real zero-delta / release-gate decision in Steps 2-3.
action="produce"
case "$classification" in
	blocked)
		action="noop" ;;
	none)
		action="produce" ;; # fresh
	ours|adopt)
		release_changed="false"
		[ "$release_ready" = "true" ] && [ "$upstream_release" != "$pr_recorded_release" ] && release_changed="true"
		# A published release on an open draft still owes the Step 3 release gate its
		# mark-ready (Step 6) transition, even once the tag is already recorded in the body
		# (e.g. a torn prior run refreshed the body but never flipped the draft, or the PR
		# was converted back to draft). Never let a caught-up state early-noop that pending
		# transition: produce so the agent can flip the draft to Ready.
		release_pending="false"
		[ "$release_ready" = "true" ] && [ "$pr_is_draft" = "true" ] && release_pending="true"
		if [ "$classification" = "ours" ] \
			&& [ -n "$upstream_sha" ] && [ -n "$pr_recorded_sha" ] \
			&& [ "$upstream_sha" = "$pr_recorded_sha" ] \
			&& [ "$release_changed" != "true" ] \
			&& [ "$release_pending" != "true" ]; then
			action="noop"
		else
			action="produce"
		fi ;;
esac

pr_state="open"; [ -z "$pr" ] && pr_state="none"; write_target "$pr" "$pr_state" "$pr_is_draft" "$pr_recorded_sha" "$pr_recorded_release" "$classification" "$action"
step_summary "$classification" "$action" "${pr:-}" "$pr_recorded_sha"

echo "Setup: classification=${classification} action=${action} pr=${pr:-<none>} pr_branch=${PR_BRANCH:-<none>} recorded_sha=${pr_recorded_sha:-<none>} upstream_sha=${upstream_sha:-<none>} release_ready=${release_ready}"
echo "-- target.json --"; jq '.' "$target_file"
