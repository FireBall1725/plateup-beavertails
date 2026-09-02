#!/usr/bin/env bash
# SPDX-License-Identifier: AGPL-3.0-only
# Copyright (C) 2026 FireBall1725
#
# Builds a release DLL and zips it with the asset bundle, laid out the way the
# Workshop uploader expects. Upload is manual; see WORKSHOP.md.
#
#   ./scripts/package.sh            -> 0.0.0-dev, a local build claiming no release
#   ./scripts/package.sh 26.8.3     -> a real release, normally passed by CI from the tag
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

dotnet="${DOTNET:-dotnet}"
command -v "$dotnet" >/dev/null 2>&1 || dotnet="$HOME/.dotnet/dotnet"
export DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}"
export DOTNET_ROLL_FORWARD="${DOTNET_ROLL_FORWARD:-LatestMajor}"

# The version comes from the caller, not from a file. CI passes the tag; anything else is
# not a release and says so rather than inventing a YY.M number for one that does not exist.
version="${1:-${VERSION:-0.0.0-dev}}"

if [[ "$version" != "0.0.0-dev" && ! "$version" =~ ^[0-9]{2}\.([1-9]|1[0-2])\.[0-9]+$ ]]; then
  echo "package: $version is neither YY.M.revision nor 0.0.0-dev" >&2
  exit 1
fi

bundle="assets/beavertails.assets"
[[ -f "$bundle" ]] || { echo "package: $bundle is missing" >&2; exit 1; }

# GAME_DIR and WORKSHOP_DIR let CI point the build at a steamcmd download. Unset, the
# csproj falls back to the local install.
build_args=( -c Release -p:DebugTools=false -p:SkipDeploy=true "-p:Version=$version" --nologo )
[[ -n "${GAME_DIR:-}" ]] && build_args+=( "-p:GameDir=$GAME_DIR" )
[[ -n "${WORKSHOP_DIR:-}" ]] && build_args+=( "-p:WorkshopDir=$WORKSHOP_DIR" )

echo "==> building BeaverTails $version without debug tools"
"$dotnet" build "${build_args[@]}"

# Look only under Release. Searching all of bin/ would happily find a stale Debug build,
# which is the one with the hotkeys in it.
dll="$(find bin/Release -name BeaverTails.dll -print -quit 2>/dev/null || true)"
[[ -n "$dll" ]] || { echo "package: build produced no BeaverTails.dll under bin/Release" >&2; exit 1; }

# A release build must not bind hotkeys. DebugTools=false compiles the picker systems out,
# so their type names should be absent from the assembly; if they are present the flag did
# not take and the build would ship F3, F4 and F5 bound in someone else's game.
for symbol in CardPickerSystem AppliancePickerSystem ForceEndOfDaySystem BrownieDiagnosticSystem; do
  if strings "$dll" 2>/dev/null | grep -qx "$symbol"; then
    echo "package: $symbol is still in the assembly, DebugTools=false did not take" >&2
    exit 1
  fi
done

# The uploader ships $modDirectory/content and nothing else, while the metadata file stays
# in the parent. Mirror that here so the zip can be extracted straight over a mod folder.
staging="$(mktemp -d)"
trap 'rm -rf "$staging"' EXIT
mkdir -p "$staging/BeaverTails/content"
cp "$dll" "$staging/BeaverTails/content/"
cp "$bundle" "$staging/BeaverTails/content/"
# Which Workshop item this build belongs to. Overridable so a test item can be uploaded
# without the zip claiming to be the live one.
workshop_id="${WORKSHOP_ITEM_ID:-3784683757}"
printf '{\n    "steamWorkshopItemID": "%s"\n}\n' "$workshop_id" > "$staging/BeaverTails/plateup_mod_metadata.json"

mkdir -p dist
out="$repo_root/dist/BeaverTails-$version.zip"
rm -f "$out"
(cd "$staging" && zip -qr "$out" BeaverTails)

echo "==> $out"
unzip -l "$out"
