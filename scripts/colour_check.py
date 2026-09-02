#!/usr/bin/env python3
# SPDX-License-Identifier: AGPL-3.0-only
# Copyright (C) 2026 FireBall1725

"""Fails when two things a player has to tell apart are the same colour.

    ./scripts/colour_check.py              # report and exit non-zero on a failure
    ./scripts/colour_check.py --list       # every colour it found, no checking
    ./scripts/colour_check.py --report     # every pair in every group, closest first

This exists because the mod shipped with a cheesecake spread and a vanilla icing at
(0.94, 0.90, 0.80) and (0.97, 0.95, 0.88), and an apple filling and a maple sugar within
0.04 of each other on every channel. A player who does not use colourblind mode reported
all four as indistinguishable, and nothing in the build caught it, because nothing in the
build looks at colours.

CI can run this. CI cannot compile the mod, because that needs the game's assemblies, so
this is one of the few real checks available on a hosted runner.

Colours are compared as CIE76 dE in Lab, treating the authored floats as sRGB. Channel
deltas are the wrong measure: (0.00, 0.02, 0.07) and (0.07, 0.02, 0.00) are the same size
in RGB and nothing like the same size to an eye.

Only colours inside a GROUP are compared. Two colours that are never on screen together
are allowed to match, and comparing everything to everything would bury the real hits.
"""

import argparse
import math
import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parent.parent
SRC = ROOT / "src"

# Anything a player has to separate at a glance, while moving. Each entry is an id from
# --list: "Class.Member" for a named Color static, "Class#0" for an unnamed literal,
# numbered in source order within its class.
#
# Naming a literal is always the better fix than adding a "#n" here, because a "#n" moves
# when a sibling literal is added above it.
#
# A group may carry its own threshold. The topping bin glass has one because those three
# are meant to be near-neutral so the sweets inside them read; the contents are what tells
# those bins apart, not the glass, and holding them to the food threshold would report a
# deliberate decision as a defect every run.
GROUPS = {
    "spread jars, their racks and their portions": [
        "HazelnutJarItem.HazelnutColour",
        "PeanutJarItem.PeanutColour",
        "CheesecakeJarItem.CheesecakeColour",
        "PistachioJarItem.PistachioColour",
        "VanillaIcingJarItem.IcingColour",
    ],
    "what is in the pot": [
        "PotWithSugarItem#0",
        "PotWithMilkItem#0",
        "PotWithAppleItem.Filling",
        "MapleSugarItem.Colour",
        "PotWithCaramelItem.Amber",
        "PotWithGravyRawItem#0",
        "CheeseCurdsItem.Curd",
        "PotWithBurnedItem.Charcoal",
    ],
    "coated tails": [
        "BeaverTailCookedItem#0",
        # BeaverTailClassicItem.Colour is deliberately absent: it tints the unlock card's art
        # and nothing else. The Classic and Killaloe both wear CookedColour now and are told
        # apart by their dusting meshes, so holding that value against the tails would be
        # comparing a card icon to a pastry.
        "BeaverTailCheesecakeCoatedItem.Colour",
        "BeaverTailPeanutCoatedItem.Colour",
        "HazelnutJarItem.HazelnutColour",
        "VanillaIcingJarItem.IcingColour",
        "BeaverTailPistachioHoneyItem.Honeyed",
        "BeaverTailAppleItem.Colour",
        "BeaverTailStrawberryCheesecakeItem.Cracker",
        "MapleButterItem.Colour",
    ],
    "scatters and drizzles on a tail": [
        "CrushedOreoItem.Cookie",
        "PistachioCrumbsItem.Green",
        "SkorBitsItem.Toffee",
        "WhiteChocolateChunksItem.Cream",
        "BeaverTailCocoVanilItem.Sauce",
        "ReesesPiecesItem.CandyOrange",
        "BeaverTailBananaramaItem.BananaFlesh",
    ],
    "topping bin glass": [
        "ToppingBinAppliance.JarColour",
        "SkorBinAppliance.JarColour",
        "PistachioBinAppliance.JarColour",
        "OreoBinAppliance.JarColour",
        "WhiteChocolateBinAppliance.JarColour",
    ],
}

# Named pairs that must separate, where a GROUP would be the wrong shape because only these
# two are ever seen against each other.
#
# A topping bin is a jar with a heap inside it, so the heap has to read against its own
# glass. The white chocolate bin is why this check exists: cream chunks in the default
# near-white jar sat at dE 20 and the chunks read as part of the jar. Every other bin was
# already past 55, so nothing flagged it until the bins were rendered side by side.
PAIRS = [
    ("Reese's bin glass vs its contents",
     "ToppingBinAppliance.JarColour", "ReesesPiecesItem.CandyOrange", 25.0),
    ("Skor bin glass vs its contents",
     "SkorBinAppliance.JarColour", "SkorBitsItem.Toffee", 25.0),
    ("cookie bin glass vs its contents",
     "OreoBinAppliance.JarColour", "CrushedOreoItem.Cookie", 25.0),
    ("pistachio bin glass vs its contents",
     "PistachioBinAppliance.JarColour", "PistachioCrumbsItem.Green", 25.0),
    ("white chocolate bin glass vs its contents",
     "WhiteChocolateBinAppliance.JarColour", "WhiteChocolateChunksItem.Cream", 25.0),
]

THRESHOLDS = {
    "topping bin glass": 5.0,
}

# Pairs that are genuinely too close and are being fixed by geometry instead of by tint.
# A waiver is a promise, not a shrug: it names the work that retires it, and it is deleted
# when that work lands. The report prints them so a stale one cannot sit here unnoticed.
#
# Empty. Both entries retired when the Classic got its own cinnamon and sugar dusting meshes
# mapped through ItemGroupView.ComponentGroups, which is why BeaverTailClassicItem.Colour is
# gone from src/ entirely: the tail wears the plain cooked colour and the dustings carry the
# difference.
WAIVED = {}

# CIE76 dE. Around 2 is "a careful eye can just see it side by side"; a moving player at
# PlateUp's camera distance needs far more than that. Per-group overrides in THRESHOLDS.
THRESHOLD = 14.0

COLOUR = re.compile(r"new Color\(\s*([0-9.]+)f,\s*([0-9.]+)f,\s*([0-9.]+)f\s*\)")
CLASS = re.compile(r"^\s*(?:public|internal|private|protected|abstract|static|sealed|\s)*"
                   r"class\s+(\w+)")
MEMBER = re.compile(r"(?:public|private|protected|internal|static|override|virtual|const|"
                    r"readonly|\s)*Color\s+(\w+)\s*(?:=>|=)")


def read_colours():
    """Every Color literal in src/, keyed by a stable id. Shared with art/ previews."""
    found = {}
    for path in sorted(SRC.rglob("*.cs")):
        current = "?"
        anonymous = {}
        for number, line in enumerate(path.read_text().splitlines(), 1):
            header = CLASS.match(line)
            if header:
                current = header.group(1)

            literal = COLOUR.search(line)
            if not literal:
                continue

            named = MEMBER.search(line)
            if named:
                key = f"{current}.{named.group(1)}"
            else:
                index = anonymous.get(current, 0)
                anonymous[current] = index + 1
                key = f"{current}#{index}"

            found[key] = {
                "rgb": tuple(float(v) for v in literal.groups()),
                "where": f"{path.relative_to(ROOT)}:{number}",
            }
    return found


def to_lab(rgb):
    def linear(c):
        return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4

    r, g, b = (linear(c) for c in rgb)

    # sRGB to XYZ, D65
    x = (r * 0.4124 + g * 0.3576 + b * 0.1805) / 0.95047
    y = (r * 0.2126 + g * 0.7152 + b * 0.0722) / 1.00000
    z = (r * 0.0193 + g * 0.1192 + b * 0.9505) / 1.08883

    def f(t):
        return t ** (1.0 / 3.0) if t > 0.008856 else (7.787 * t) + (16.0 / 116.0)

    fx, fy, fz = f(x), f(y), f(z)
    return (116.0 * fy - 16.0, 500.0 * (fx - fy), 200.0 * (fy - fz))


def distance(first, second):
    return math.dist(to_lab(first), to_lab(second))


def pairs(colours, members):
    known = [m for m in members if m in colours]
    for i, a in enumerate(known):
        for b in known[i + 1:]:
            yield a, b, distance(colours[a]["rgb"], colours[b]["rgb"])


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--list", action="store_true", help="print every colour found")
    parser.add_argument("--report", action="store_true", help="print every pair, closest first")
    args = parser.parse_args()

    colours = read_colours()

    if args.list:
        for key in sorted(colours):
            r, g, b = colours[key]["rgb"]
            print(f"{key:<50} {r:.2f} {g:.2f} {b:.2f}   {colours[key]['where']}")
        return 0

    named = [m for members in GROUPS.values() for m in members]
    named += [m for _, a, b, _ in PAIRS for m in (a, b)]
    missing = sorted({m for m in named if m not in colours})

    failures = []
    for name, members in GROUPS.items():
        limit = THRESHOLDS.get(name, THRESHOLD)
        scored = sorted(pairs(colours, members), key=lambda p: p[2])
        if args.report:
            print(f"\n{name}  (dE {limit})")
            for a, b, gap in scored:
                if frozenset((a, b)) in WAIVED:
                    mark = "WAIV"
                elif gap < limit:
                    mark = "FAIL"
                else:
                    mark = "ok  "
                print(f"  {mark} dE {gap:6.1f}   {a}  vs  {b}")
        failures.extend(
            (name, a, b, gap)
            for a, b, gap in scored
            if gap < limit and frozenset((a, b)) not in WAIVED
        )

    if args.report and PAIRS:
        print("\nnamed pairs")
    for label, a, b, limit in PAIRS:
        if a not in colours or b not in colours:
            continue
        gap = distance(colours[a]["rgb"], colours[b]["rgb"])
        if args.report:
            mark = "FAIL" if gap < limit else "ok  "
            print(f"  {mark} dE {gap:6.1f}   {a}  vs  {b}")
        if gap < limit:
            failures.append((label, a, b, gap))

    if missing:
        print("Named in GROUPS or PAIRS but not found in src/. Renamed, or moved to another class:")
        for member in missing:
            print(f"  {member}")
        print()

    if failures:
        print(f"{len(failures)} pair(s) too close to tell apart:")
        for name, a, b, gap in sorted(failures, key=lambda f: f[3]):
            print(f"  dE {gap:5.1f}  {a}")
            print(f"            {b}")
            print(f"            ({name})")
    elif not args.report:
        print("All groups clear.")

    if WAIVED:
        print(f"\n{len(WAIVED)} waived pair(s), each waiting on geometry rather than a tint:")
        for pair, reason in WAIVED.items():
            print(f"  {' / '.join(sorted(pair))}")
            print(f"      {reason}")

    return 1 if (failures or missing) else 0


if __name__ == "__main__":
    sys.exit(main())
