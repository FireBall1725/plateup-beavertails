#!/usr/bin/env bash
# SPDX-License-Identifier: AGPL-3.0-only
# Copyright (C) 2026 FireBall1725
#
# Formats the C# sources. Run this rather than dotnet format on its own: Roslyn pads each
# parenthesis pair independently and leaves `Foo( Bar( x ) )`, and house style keeps
# adjacent parens tight, so a second pass closes those gaps.
#
# --check verifies without writing, for use before a commit.
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

dotnet="${DOTNET:-dotnet}"
command -v "$dotnet" >/dev/null 2>&1 || dotnet="$HOME/.dotnet/dotnet"
export DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}"
export DOTNET_ROLL_FORWARD="${DOTNET_ROLL_FORWARD:-LatestMajor}"

check=""
[[ "${1:-}" == "--check" ]] && check=1

# Not mapfile: macOS ships bash 3.2, which does not have it.
sources=()
while IFS= read -r f; do
  sources+=( "$f" )
done < <( find src -name '*.cs' | sort )

if [[ -n "$check" ]]; then
  before="$(mktemp -d)"
  trap 'rm -rf "$before"' EXIT
  for f in "${sources[@]}"; do
    mkdir -p "$before/$(dirname "$f")"
    cp "$f" "$before/$f"
  done
fi

"$dotnet" format whitespace BeaverTails.csproj --no-restore
if [[ -n "$check" ]]; then
  python3 scripts/tighten_parens.py "${sources[@]}" >/dev/null
else
  python3 scripts/tighten_parens.py "${sources[@]}"
fi

if [[ -n "$check" ]]; then
  status=0
  for f in "${sources[@]}"; do
    if ! diff -q "$before/$f" "$f" >/dev/null; then
      echo "not formatted: $f" >&2
      cp "$before/$f" "$f"
      status=1
    fi
  done
  [[ $status -eq 0 ]] && echo "all ${#sources[@]} files are formatted"
  exit $status
fi

echo "formatted ${#sources[@]} files"
