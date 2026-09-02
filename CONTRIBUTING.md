# Contributing to Beaver Tails

Thanks for your interest. This document covers how to submit changes, what a PR needs, and the
terms your contribution is made under.

Using an AI coding agent? [AGENTS.md](./AGENTS.md) has the architecture, the build loop, and the
mechanics that are easy to get wrong. Point your tool at it, and read what it writes before you
open the PR.

## Before you start

- Check the open issues first. Your change may already be planned differently.
- For anything beyond a typo, open an issue before writing code. Recipe balance and card design
  are opinionated, and it is better to disagree in an issue than in a finished branch.
- A change you cannot test in game is a change nobody can review. Say so in the PR if you were
  unable to run it.

## Development setup

```bash
git clone git@github.com:FireBall1725/plateup-beavertails.git
cd plateup-beavertails
dotnet build
```

You need the .NET SDK and a real PlateUp install. The paths at the top of `BeaverTails.csproj`
point at the game's `PlateUp_Data/Managed` and at the Workshop folder holding KitchenLib,
IngredientLib and Fryer Appliance. Edit them for your machine; do not commit your local paths.

The build copies the DLL and the asset bundle straight into the game's mod folder, so the loop
is build, launch, check `Player.log`.

## Making changes

- One PR, one change. A recipe and a build fix are two PRs.
- Build with `-p:DebugTools=false` before you call something done. Hotkeys are on by default and
  must not ship.
- Match the surrounding code. Items live in `src/Items/`, cards in `src/RecipeCards.cs`, and
  every game data object is looked up through `src/Gdo.cs` rather than by hand.
- Run `./scripts/format.sh` before you commit. Two-space indent, braces on the same line,
  parens padded inside (`Foo( a, b )`) and tight against each other (`Foo( Bar( x ))`).
  Do not run `dotnet format` on its own; it cannot express the adjacent-paren rule.
- Never look up an IngredientLib object from a field initialiser. It resolves before that mod
  has registered and you get a null with no useful stack trace.
- Every source file opens with the SPDX header:

  ```csharp
  // SPDX-License-Identifier: AGPL-3.0-only
  // Copyright (C) 2026 FireBall1725
  ```

## Comments

Comment the parts that surprised you, not the parts the code already says. `// increment the
counter` above `i++` is noise. The reason a process is hob-only, or why an ingredient is listed
on three cards instead of one, is what a reader cannot reconstruct.

Write them plainly. No filler openers, no "note that", no marketing adjectives.

## Commit messages

Short and imperative, with the scope up front: `fix(poutine): curds no longer merge into the
gravy`, `feat(cards): add Pistach-OH`.

Every commit needs a DCO sign-off. See below.

## Pull requests

- Rebase on `main` before opening.
- The title becomes a release-note line, so write it for someone reading a changelog.
- Explain the why in the body. A paragraph and a few bullets is plenty; no "## Summary" or
  "## Test plan" scaffolding.
- Do not hand-edit a changelog. Release notes are generated from PR titles.
- Say which PlateUp version you tested against. The game moves and patches bind to methods by
  name.

## Licence

AGPL-3.0-only ([LICENSE](./LICENSE)). Contributions are accepted under the same licence. Nothing
is assigned to the maintainer and there is no separate commercial-relicensing grant.

## Sign your commits (DCO)

Every commit must carry a `Signed-off-by:` trailer certifying the
[Developer Certificate of Origin 1.1](./DCO). It says you have the right to contribute the code
and are fine with it shipping under the project's licence.

Pass `-s` to `git commit`:

```bash
git commit -s -m "feat(cards): add Pistach-OH"
```

Forgot on one commit:

```bash
git commit --amend -s --no-edit
```

Forgot on several:

```bash
git rebase --signoff main
```

Do not add `Co-Authored-By` trailers for AI tools, or "Generated with" footers, in commits or PR
bodies.

## Code of conduct

Be decent. Assume good faith. Technical disagreements are fine; personal attacks are not.
