# Installing the Beaver Tails mod

## What to send

Two files, both in `<PlateUp install>/Mods/BeaverTails/`:

- `BeaverTails.dll` - the mod
- `beavertails.assets` - the models

Zip that folder and send it. Nothing else in the repo is needed to play; `src/` only matters
for rebuilding.

## Where it goes

The folder keeps its name and sits under `Mods/` in the PlateUp install directory.

| Platform | Path |
|---|---|
| Windows (Steam) | `C:\Program Files (x86)\Steam\steamapps\common\PlateUp\Mods\BeaverTails\` |
| Mac (GameHub) | `<steam library>/steamapps/common/PlateUp/PlateUp/Mods/BeaverTails/` |

Note the doubled `PlateUp/PlateUp` on the Mac side. The outer folder is the Steam depot; the
inner one is the Windows game the Wine container runs.

## Required mods

All three have to be subscribed on the Steam Workshop before this one will load:

- **KitchenLib** - workshop ID `2898069883`
- **Fryer Appliance** - workshop ID `3765792217`
- **IngredientLib** - workshop ID `2913877103`

The fryer isn't optional. Beaver tails cook with `KitchenFryer.FryMe`, a process that mod
defines, so without it the fry step has no appliance that can perform it and the game reports
an impossible menu.

IngredientLib isn't optional either. It supplies honey, crackers and vinegar, none of which the
base game has, and it owns the cinnamon, banana and caramel this mod uses. Without it the
recipes that need them have nothing to resolve to.

## If KitchenLib fails to load

On this machine KitchenLib needed `Microsoft.Bcl.HashCode.dll` dropped into
`PlateUp_Data/Managed/`, because it wasn't present in the install. If your friend sees
KitchenLib fail post-activate with a missing dependency, that's the file. Copy it out of the
same `Managed` folder here.

A stale **Cards Manager** subscription also breaks KitchenLib, by poisoning
`Assembly.GetTypes()` during startup. Unsubscribing fixed it here.

## Multiplayer

Everyone in the lobby needs identical mod files. A mismatched `BeaverTails.dll` between host
and client desyncs the game data IDs, since KitchenLib assigns them from the mod GUID and the
object's unique name.

## Rebuilding from source

```
dotnet build
```

This compiles against the game DLLs and copies both output files into `Mods/BeaverTails/`
automatically. The asset bundle is committed, so rebuilding the models is only needed when a
model changes, and the Blender sources are not in the repo yet.
