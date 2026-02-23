#!/usr/bin/env bash
set -euo pipefail

usage() {
  cat <<'USAGE'
Usage: bump-version-commit-tag.sh [options]

Bump Directory.Build.props version, commit all current changes, push branch,
and create/push matching tag (vX.Y.Z).

Options:
  --bump <x|y|z|major|minor|patch>  Bump type (interactive if omitted)
  --type <token>                     Commit type token (default: CI)
  --summary <text>                   Commit summary (default: "Release v<new-version>")
  --branch <name>                    Require current branch name
  --yes                              Skip confirmation prompt
  --dry-run                          Print actions without changing git state
  --no-tag                           Skip tag creation/push
  --no-push                          Skip branch/tag push
  -h, --help                         Show this help
USAGE
}

require_cmd() {
  if ! command -v "$1" >/dev/null 2>&1; then
    echo "Error: required command not found: $1" >&2
    exit 1
  fi
}

normalize_bump() {
  case "${1,,}" in
    x|major) echo "x" ;;
    y|minor) echo "y" ;;
    z|patch) echo "z" ;;
    *)
      echo "Error: invalid bump type '$1'. Use x/y/z (or major/minor/patch)." >&2
      exit 1
      ;;
  esac
}

next_version() {
  local current="$1"
  local bump="$2"
  local major minor patch

  if [[ ! "$current" =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
    echo "Error: unsupported version format '$current'. Expected X.Y.Z" >&2
    exit 1
  fi

  IFS='.' read -r major minor patch <<< "$current"

  case "$bump" in
    x)
      major=$((major + 1))
      minor=0
      patch=0
      ;;
    y)
      minor=$((minor + 1))
      patch=0
      ;;
    z)
      patch=$((patch + 1))
      ;;
  esac

  echo "${major}.${minor}.${patch}"
}

update_version_file() {
  local file="$1"
  local new_version="$2"
  local tmp
  tmp="$(mktemp)"

  awk -v new_version="$new_version" '
    BEGIN { replaced = 0 }
    {
      if (replaced == 0 && $0 ~ /<Version>[0-9]+\.[0-9]+\.[0-9]+<\/Version>/) {
        sub(/<Version>[0-9]+\.[0-9]+\.[0-9]+<\/Version>/, "<Version>" new_version "</Version>")
        replaced = 1
      }
      print
    }
    END {
      if (replaced == 0) {
        print "Error: <Version>X.Y.Z</Version> not found in file" > "/dev/stderr"
        exit 2
      }
    }
  ' "$file" > "$tmp"

  mv "$tmp" "$file"
}

prompt_if_empty() {
  local var_name="$1"
  local prompt="$2"
  local default_value="$3"
  local value=""

  read -r -p "$prompt" value
  if [[ -z "$value" ]]; then
    value="$default_value"
  fi
  printf -v "$var_name" '%s' "$value"
}

require_cmd git
require_cmd grep
require_cmd awk

BUMP=""
TYPE_TOKEN="CI"
SUMMARY=""
REQUIRE_BRANCH=""
ASSUME_YES=0
DRY_RUN=0
NO_TAG=0
NO_PUSH=0

while [[ $# -gt 0 ]]; do
  case "$1" in
    --bump)
      BUMP="$2"
      shift 2
      ;;
    --type)
      TYPE_TOKEN="$2"
      shift 2
      ;;
    --summary)
      SUMMARY="$2"
      shift 2
      ;;
    --branch)
      REQUIRE_BRANCH="$2"
      shift 2
      ;;
    --yes)
      ASSUME_YES=1
      shift
      ;;
    --dry-run)
      DRY_RUN=1
      shift
      ;;
    --no-tag)
      NO_TAG=1
      shift
      ;;
    --no-push)
      NO_PUSH=1
      shift
      ;;
    -h|--help)
      usage
      exit 0
      ;;
    *)
      echo "Error: unknown option '$1'" >&2
      usage >&2
      exit 1
      ;;
  esac
done

repo_root="$(git rev-parse --show-toplevel 2>/dev/null || true)"
if [[ -z "$repo_root" ]]; then
  echo "Error: not inside a git repository." >&2
  exit 1
fi
cd "$repo_root"

current_branch="$(git rev-parse --abbrev-ref HEAD)"
if [[ "$current_branch" == "HEAD" ]]; then
  echo "Error: detached HEAD. Checkout a branch first." >&2
  exit 1
fi

if [[ -n "$REQUIRE_BRANCH" && "$current_branch" != "$REQUIRE_BRANCH" ]]; then
  echo "Error: current branch is '$current_branch', expected '$REQUIRE_BRANCH'." >&2
  exit 1
fi

version_file="Directory.Build.props"
if [[ ! -f "$version_file" ]]; then
  echo "Error: $version_file not found in repository root." >&2
  exit 1
fi

current_version="$(grep -oPm1 '(?<=<Version>)[^<]+' "$version_file" | tr -d '[:space:]' || true)"
if [[ -z "$current_version" ]]; then
  echo "Error: could not resolve <Version> from $version_file." >&2
  exit 1
fi

if [[ -z "$BUMP" ]]; then
  prompt_if_empty BUMP "Select bump type [x=major, y=minor, z=patch] (default z): " "z"
fi
BUMP="$(normalize_bump "$BUMP")"

new_version="$(next_version "$current_version" "$BUMP")"
tag_name="v${new_version}"

if git rev-parse "$tag_name" >/dev/null 2>&1; then
  echo "Error: local tag '$tag_name' already exists." >&2
  exit 1
fi

if git ls-remote --exit-code --tags origin "refs/tags/${tag_name}" >/dev/null 2>&1; then
  echo "Error: remote tag '$tag_name' already exists on origin." >&2
  exit 1
fi

if [[ -z "$SUMMARY" ]]; then
  SUMMARY="Release v${new_version}"
fi

if [[ $ASSUME_YES -eq 0 ]]; then
  echo ""
  echo "Repository : $repo_root"
  echo "Branch     : $current_branch"
  echo "Version    : $current_version -> $new_version"
  echo "Tag        : $tag_name"
  echo "Commit msg : [v${new_version}] [${TYPE_TOKEN}] ${SUMMARY}"
  echo "Push       : $([[ $NO_PUSH -eq 0 ]] && echo yes || echo no)"
  echo "Create tag : $([[ $NO_TAG -eq 0 ]] && echo yes || echo no)"
  echo ""
  read -r -p "Proceed? [y/N]: " confirm
  if [[ ! "$confirm" =~ ^[Yy]$ ]]; then
    echo "Cancelled."
    exit 0
  fi
fi

if [[ $DRY_RUN -eq 1 ]]; then
  echo "[DRY RUN] Would update $version_file to $new_version"
  echo "[DRY RUN] Would run: git add -A"
  echo "[DRY RUN] Would run: git commit -m \"[v${new_version}] [${TYPE_TOKEN}] ${SUMMARY}\""
  if [[ $NO_PUSH -eq 0 ]]; then
    echo "[DRY RUN] Would run: git push origin $current_branch"
  fi
  if [[ $NO_TAG -eq 0 ]]; then
    echo "[DRY RUN] Would run: git tag -a $tag_name -m $tag_name"
    if [[ $NO_PUSH -eq 0 ]]; then
      echo "[DRY RUN] Would run: git push origin $tag_name"
    fi
  fi
  exit 0
fi

update_version_file "$version_file" "$new_version"

git add -A

if git diff --cached --quiet; then
  echo "Error: no staged changes after bump. Nothing to commit." >&2
  exit 1
fi

git commit -m "[v${new_version}] [${TYPE_TOKEN}] ${SUMMARY}"

if [[ $NO_PUSH -eq 0 ]]; then
  git push origin "$current_branch"
fi

if [[ $NO_TAG -eq 0 ]]; then
  git tag -a "$tag_name" -m "$tag_name"
  if [[ $NO_PUSH -eq 0 ]]; then
    git push origin "$tag_name"
  fi
fi

echo "Done: version bumped to $new_version"
