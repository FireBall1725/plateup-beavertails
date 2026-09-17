# AGENTS.md

Guidance for AI coding agents working in `plateup-beavertails`. Humans should read
[CONTRIBUTING.md](./CONTRIBUTING.md) first; this file assumes you have and does not repeat it.

## What this repo is

A PlateUp! mod. It adds a beaver tail as a main dish and fifteen unlock cards that build on it.
[README.md](./README.md) has the menu and the item chain; read it before changing a recipe.

A mod is a plain .NET class library targeting `net472`. It compiles against the game's own
assemblies and is loaded by KitchenLib at runtime. There is no test suite and no CI build,
for reasons given under "CI" below.

## Layout

```
src/Mod.cs              BaseMod entry point; every AddGameDataObject call lives here
src/Gdo.cs              lookup helpers for game data objects, ours and other mods'
src/BeaverTailsDish.cs  the Dish GDO: menu items, card list, starting set
src/RecipeCards.cs      all fifteen unlock cards
src/Items/              the item chain, one file per branch
src/*System.cs          runtime systems, including the dev-only pickers
assets/                 the built Unity asset bundle, committed on purpose
scripts/package.sh      release build and zip
```

## Build

```bash
dotnet build                        # dev build, F3/F4/F5 hotkeys compiled in
dotnet build -p:DebugTools=false    # release; the only build fit to upload
./scripts/package.sh                # release build plus the upload zip
```

The paths at the top of `BeaverTails.csproj` point at a local PlateUp install and Workshop
folder. They are machine-specific. Do not commit changes to them as part of an unrelated PR.

Verify by launching the game and reading `Player.log`. A build that compiles proves almost
nothing here, because Harmony binds patches by method name at runtime and the game's assemblies
change between versions.

## Mechanics that are easy to get wrong

These each cost a real debugging session. They are not obvious from the code.

**A process and a split are mutually exclusive on one item.** Adding a Cook process to an item
that splits removes the take-out interaction entirely, with no key working. That is why vanilla
doughnuts cannot burn and why the burn step is on the fried tail rather than in a pot.

**Frying is not a process on food.** It is an `ItemGroup` pairing the raw item with the fryer,
and the group carries the process. `FryMe` comes from the Fryer Appliance mod.

**Declaring only the process you think you need hands you the wrong appliance.** The fryer also
provides `Cook`, so a recipe requiring `Cook` gets no hob and no oven, and the pot ends up in
the oil. `CaramelizeProcess` is hob-only for exactly this reason.

**`UnlockInfo` derives from Odin's `SerializedScriptableObject`.** Build it with
`ScriptableObject.CreateInstance<UnlockInfo>()`, never `new`. A card name renders as `?` unless
`InfoList` is set, and `InfoList` is a list of tuples, not a plain list.

**Do not override `Recipe` on the Dish.** KitchenLib loops every `Locale` and dereferences the
recipe text with no null check, which hard-crashes the game with no managed stack trace. Recipe
text goes in through `RegisterRecipeTextSystem` instead.

**Anything writing GameData must derive from `GenericSystemBase`, not `GameSystemBase`.**
`GameSystemBase` carries a host-only marker, so it never runs on a multiplayer client and the
symptom is a client with blank recipes rather than an error.

**Look up IngredientLib objects lazily.** A field initialiser resolves before that mod has
registered. Use the `Gdo.Lib` helpers, which defer.

**Prefab clones must stay active.** `Object.Instantiate` copies `activeSelf`, so a clone left
inactive spawns an invisible item. Park clones under an inactive parent instead.

**Colour lives in `_Color0`, not `_Color`.** The game's Simple Flat shader has no `_Color`, so
`material.color = x` is a silent no-op.

**Clearing another item's `CPreventItemMerge` opens every merge, not the one you want.**
Removing `NoMerge` from the vanilla Brownie let a brownie tray wrap a brownie and fill with raw
dough. No `MergeCondition` fixes it, because `AttemptItemMerge` reaches the tray through a
branch that needs only `CanComp` on the brownie side, which is the flag BrWOWnie needs too.
`BrownieMergePatch` relaxes the condition for that one pairing instead.

**A partial `ItemGroup` becomes a complete one the moment it is plated.** `AttemptComponentMerge`
creates the result with `is_partial` taken from the PLATE's satisfaction, not the food's, and
nothing rechecks it afterwards. A tail carrying one of the two dustings served as a finished
Cinnamon Sugar Tail because of this, and the plate's component list keeps the tail as a single id,
so the view cannot tell the variants apart either. Build each stage as its own item with one
mandatory `Min = 2, Max = 2` set, so no half-built state exists to launder.

**The plate's side slot takes any item, not the sides you meant.** `CanContainSide` on the group
plus `IsMergeableSide` on the item is the whole test in `IsGroupSatisfied`; there is no allow-list.
A sliced lemon dropped onto a finished plate was swallowed, drew nothing, and `IsRequestSatisfied`
still reported the dish as correct. `PlatedMergePatch` refuses that pairing by name, because an
allow-list would also have to name the maple syrup serving and every condiment added after it.

**`Appliance.ApplianceProcesses.Speed` is per appliance, and a custom process has to copy it.**
`ProcessesView` turns the entry into `Speed / Duration`, so writing a flat 1 makes a Danger Hob and
a Safety Hob cook a pot at the same rate while vanilla's own Cook keeps 2x and starter pace on
those same two appliances. `Gdo.TeachProcess` takes a `speedFrom` process and reads the donor's
value off each appliance.

**The bin reads `DisposesTo`, not `DirtiesTo`.** `AcceptIntoBin.AcceptTransfer` hands back
`Item.DisposesTo` and nothing else. `DirtiesTo` only covers a customer finishing a meal, so a
plated dish that set one and not the other took the plate into the bin along with the food.

**Adding a helper method directly above a patched method steals its attributes.** C# attributes
bind to the next declaration, so an inserted helper takes the `[HarmonyPatch]` and the mod fails
to load with a parameter-binding error naming a method you did not patch. Attach helpers below a
field declaration, and re-check every attribute after any insertion.

## Conventions

- Every source file opens with the SPDX header and `Copyright (C) 2026 FireBall1725`. The name
  is exactly that: capital F, capital B, never paired with a first name, never the lowercase
  slug form, which is for URLs and package ids.
- Two-space indentation. Braces are one-true-brace: the opening brace stays on the line
  that opened the block, and `else`, `catch` and `finally` follow the closing brace.
- Parentheses are padded on the inside, `Foo( a, b )` and `if ( x )`, with no space between
  a name and its opening paren. Adjacent parens stay tight, `Foo( Bar( x ))`, never
  `Foo( Bar( x ) )`.
- Run `./scripts/format.sh` rather than `dotnet format` on its own. Roslyn pads each paren
  pair independently and cannot express the adjacent-paren rule, so a second pass closes
  those gaps. `./scripts/format.sh --check` verifies without writing.
- Look game data objects up through `src/Gdo.cs`. Do not scatter `GDOUtils` calls.
- Card display names are deliberate, including `mEHple`, `Pistach-OH` and `BrWOWnie`. Do not
  correct their capitalisation.
- Comment the surprising part, not the mechanical one. If the code already says it, delete the
  comment.
- No AI attribution anywhere: no `Co-Authored-By` trailer, no "Generated with" footer, in commit
  messages or PR bodies.
- Every commit needs a DCO sign-off (`git commit -s`).

## Versioning

`YY.M.revision`. The month is not zero-padded, because `26.08.1` is not valid SemVer.

Do not write a version literal anywhere. CI passes the tag as `-p:Version=`, the csproj
defaults to `0.0.0-dev` when nothing passes one, and `Mod.cs` reads it back off
`AssemblyInformationalVersion`. A second copy is a second thing to forget, and it would make
a release built from the right tag report the wrong number in game.

`CompatibleGameVersions` in `Mod.cs` is a different thing and stays a literal: changing it is
a claim that the mod has been tested against a newer PlateUp.

## CI

CI does not compile this mod, and cannot. Building it needs PlateUp's `PlateUp_Data/Managed`
assemblies, three Workshop mod DLLs, and for the models a licensed Unity 2020.3.48f1. None of
those can be committed or fetched in a hosted runner.

So the workflow in `.github/workflows/ci.yml` checks what it can without the game: the two
version strings agree, every source file carries the SPDX header, adjacent parens are tight,
and `editorconfig-checker` passes. It cannot run `dotnet format`, because loading the project
needs the game's assemblies, so the rest of the formatting is on you to run locally.

`.github/workflows/release.yml` does build the mod, on a runner that can reach Steam. It
uses steamcmd to fetch PlateUp's assemblies and the three Workshop mod DLLs with an account
that owns the game, so nothing copyrighted is committed, and it caches them keyed on the
csproj. The asset bundle is checked in, so no Unity is involved. It produces the same zip
`scripts/package.sh` does and attaches it to a GitHub Release on a tag.

It needs two secrets, `STEAM_USERNAME` and `STEAM_CONFIG_VDF`, described in that file's
header. Set the `RUNNER_LABEL` repo variable to use a self-hosted runner; unset it falls
back to `ubuntu-latest` and re-downloads the game every run.

Uploading to Steam is still manual. `ModUploader.exe` is a 61 MB NSIS self-extracting
Electron bundle with no command-line interface. `steamcmd +workshop_build_item` is the
likely automated path, but whether PlateUp's Workshop accepts an item built that way is
untested; try it against a new hidden item first, never the live one.

This is also why the workflow lives in this repo rather than calling
[FireBall1725/workflows](https://github.com/FireBall1725/workflows). The shared workflows cover
Go, Node, Python, Swift and Astro; a game mod that produces a zip is a new surface with one
repo in it, which is the same reasoning that keeps LayerLens's release workflow in-repo.

There is no CodeQL workflow because CodeQL is free on public repositories only and this repo is
private. That is a billing constraint, not a decision, and it flips if the repo ever goes
public.

## Things that will bite you

- **The Workshop uploader ships `Mods/BeaverTails/content` and nothing else.** An empty `content`
  folder fails at submit with "a generic failure from the steamworks API" while title-only
  updates keep working, which is a confusing way to learn this.
- **Do not leave a second copy of the DLL in the parent folder.** The loader reads both levels
  and two copies of the assembly is a duplicate mod GUID, which hangs HQ.
- **Multiplayer needs identical files on every machine.** KitchenLib assigns game data IDs from
  the mod GUID and each object's unique name, so a mismatched DLL desyncs those IDs.
- **The preview image cap is 1 MB.** Going over fails with the same generic Steamworks error as
  an empty content folder.
