#!/usr/bin/env bash
set -euo pipefail

usage() {
  cat <<'USAGE'
Usage: run-release-sequence.sh [sequence-options] [-- bump-script-options]

Run release flow in strict order:
1) maintenance-chores skill
2) update-changelog skill
3) bump-version-commit-tag.sh

Sequence options:
  --assume-maintenance-done   Skip interactive confirmation for step 1
  --assume-changelog-done     Skip interactive confirmation for step 2
  -h, --help                  Show this help

All other options are passed through to:
  ./.ai/skills/xerahs-release-bump-tag/scripts/bump-version-commit-tag.sh
USAGE
}

ASSUME_MAINTENANCE_DONE=0
ASSUME_CHANGELOG_DONE=0

PASSTHROUGH_ARGS=()
while [[ $# -gt 0 ]]; do
  case "$1" in
    --assume-maintenance-done)
      ASSUME_MAINTENANCE_DONE=1
      shift
      ;;
    --assume-changelog-done)
      ASSUME_CHANGELOG_DONE=1
      shift
      ;;
    -h|--help)
      usage
      exit 0
      ;;
    --)
      shift
      PASSTHROUGH_ARGS+=("$@")
      break
      ;;
    *)
      PASSTHROUGH_ARGS+=("$1")
      shift
      ;;
  esac
done

repo_root="$(git rev-parse --show-toplevel 2>/dev/null || true)"
if [[ -z "$repo_root" ]]; then
  echo "Error: not inside a git repository." >&2
  exit 1
fi
cd "$repo_root"

maintenance_skill=".ai/skills/maintenance-chores/SKILL.md"
changelog_skill=".ai/skills/update-changelog/SKILL.md"
bump_script=".ai/skills/xerahs-release-bump-tag/scripts/bump-version-commit-tag.sh"

if [[ ! -f "$maintenance_skill" ]]; then
  echo "Error: required skill file not found: $maintenance_skill" >&2
  exit 1
fi
if [[ ! -f "$changelog_skill" ]]; then
  echo "Error: required skill file not found: $changelog_skill" >&2
  exit 1
fi
if [[ ! -x "$bump_script" ]]; then
  echo "Error: required script not executable: $bump_script" >&2
  exit 1
fi

if [[ $ASSUME_MAINTENANCE_DONE -eq 0 ]]; then
  echo "Step 1 required: run maintenance chores skill first:"
  echo "  $maintenance_skill"
  read -r -p "Type 'done' after finishing step 1: " response
  if [[ "$response" != "done" ]]; then
    echo "Aborted: maintenance step not confirmed."
    exit 1
  fi
fi

if [[ $ASSUME_CHANGELOG_DONE -eq 0 ]]; then
  echo "Step 2 required: run changelog update skill second:"
  echo "  $changelog_skill"
  read -r -p "Type 'done' after finishing step 2: " response
  if [[ "$response" != "done" ]]; then
    echo "Aborted: changelog step not confirmed."
    exit 1
  fi
fi

echo "Step 3: running bump/tag automation..."
exec "$bump_script" "${PASSTHROUGH_ARGS[@]}"
