# Beaver Tails

A PlateUp! mod that adds Canadian fried dough as a main dish, plus fifteen unlock cards
that build on it.

The base dish is a beaver tail: flour and milk into dough, knead it flat, deep fry it, then
cinnamon and sugar. Everything after that is a card you pick up during a run. Each card is a
full recipe with its own ingredients and its own steps, not a topping bolted onto the end of
an existing one.

Requires [KitchenLib](https://steamcommunity.com/sharedfiles/filedetails/?id=2898069883),
[Fryer Appliance](https://steamcommunity.com/sharedfiles/filedetails/?id=3765792217) and
[IngredientLib](https://steamcommunity.com/sharedfiles/filedetails/?id=2913877103). All three
are load-bearing, and the section on dependencies below says what breaks without each.

## The base dish

Six items, and the whole mod hangs off this chain:

```
Flour + Milk                    ->  Beaver Tail Dough      (an ItemGroup, no process)
Beaver Tail Dough   --Knead 2s->     Beaver Tail - Raw
Beaver Tail - Raw   --FryMe 4s->     Beaver Tail
Beaver Tail         --FryMe 20s->    Beaver Tail - Burned  (IsBad)
Beaver Tail + Sugar             ->  Beaver Tail - Cinnamon Sugar
Cinnamon Sugar + Plate          ->  the menu item
```

Dough is its own `ItemGroup` rather than the base game's, because an item gets one process
per branch and the game's `Dough` has already spent its Knead on pie crust and its Cook on
bread. Frying is not a process on the food either: it is `KitchenFryer.FryMe`, from the Fryer
Appliance mod.

The burn step exists because a beaver tail left in the oil turns into `Beaver Tail - Burned`
after 20 seconds. Vanilla doughnuts cannot burn, since split and process are mutually
exclusive on one item and the doughnut needs its split to come out of the pot.

## The menu

Fifteen recipe cards plus the base dish. Difficulty is the number the card shows in game.

### Built on the tail

| Card | Difficulty | What it is |
| --- | --- | --- |
| Killaloe Sunrise | 3 | Cinnamon and sugar, served with a slice of lemon. |
| Hazel Amour | 3 | Chocolate hazelnut spread with a dusting of icing sugar. |
| Bananarama | 4 | Chocolate hazelnut spread topped with fresh banana slices. |
| Triple Trip | 4 | Chocolate hazelnut spread, peanut butter, and a scatter of Reese's Pieces. |
| Avalanche | 4 | Cheesecake spread, Skor bits, and a caramel drizzle. |
| mEHple | 4 | Maple butter, and maple sugar boiled in a pot. |
| Pistach-OH | 4 | Pistachio spread and honey, finished with chopped pistachios. |
| Strawberry Cheesecake | 4 | Cheesecake spread and strawberry syrup, finished with crushed crackers. |
| Apple Pie | 5 | Apple pie filling with a caramel drizzle, both stewed in a pot on the hob. |
| Coco Vanil | 5 | Vanilla icing and crushed cookies, finished with a chocolate drizzle. |
| BrWOWnie | 5 | Hazelnut spread and a piece of oven-baked brownie, finished with white chocolate chunks. |

The odd capitalisation on `mEHple`, `Pistach-OH` and `BrWOWnie` is deliberate and matches the
card in game. Do not tidy it.

### Sides

| Card | What it is |
| --- | --- |
| Fries | Chop a potato and fry the pieces. |
| Poutine | Fries under gravy and cheese curds. |
| Double Cheese Poutine | A poutine, with fried cheese curds on top of the fresh ones. |

Sides need `DishType.Side`. Setting `DishType.Extra` instead puts them on the plate as a
topping, which is not what a poutine is.

### Extra

Maple Syrup is a bottle, not a recipe. It is `DishType.Extra`, so customers ask for it once
they are already eating rather than ordering it up front.

## Shared stages, and why cards can be taken in any order

Several cards pass through the same intermediate item. Hazel Amour, Bananarama and Triple Trip
all coat the tail in hazelnut spread, and that coated stage is one item shared between them.

Each of those cards lists the hazelnut jar in its own `NewIngredients`. The duplication is
deliberate: whichever card you take first brings the jar, and a second one adds no second rack.
Drop a listing and that card arrives with no way to make it.

## Dependencies

**KitchenLib** registers every custom game data object. Nothing loads without it.

**Fryer Appliance** owns the `FryMe` process. Without it the fry step has no appliance that can
perform it, and the game reports an impossible menu rather than naming the missing mod.

**IngredientLib** supplies honey, crackers and vinegar, which the base game does not have, and
owns the canonical cinnamon, banana and caramel this mod uses instead of duplicating. Recipes
that need them have nothing to resolve to without it.

Both Fryer Appliance and IngredientLib belong in the Workshop **required items** field as well
as in the description. A subscriber missing one gets a broken menu and no explanation.

## Installing

See [INSTALL.md](./INSTALL.md) for paths on each platform and what to send someone who just
wants to play. The short version is that `BeaverTails.dll` and `beavertails.assets` go in
`<PlateUp install>/Mods/BeaverTails/`.

## Building

```bash
dotnet build                        # development build, hotkeys on
dotnet build -p:DebugTools=false    # the only build fit to upload
```

The project targets `net472` and compiles against the game's own assemblies in
`PlateUp_Data/Managed`, so the paths at the top of `BeaverTails.csproj` have to point at a real
PlateUp install. A successful build copies the DLL and the asset bundle into the game's
`Mods/BeaverTails/content/` folder.

Development builds bind F3 (card picker), F4 (appliance spawner) and F5 (force end of day).
A shipped build must not bind hotkeys, so `-p:DebugTools=false` is the explicit one.

To produce an upload-ready zip:

```bash
./scripts/package.sh
```

That runs the release build, checks the version in `src/Mod.cs` against the csproj, and writes
`dist/BeaverTails-<version>.zip` laid out the way the Workshop uploader expects.

Pushing a tag builds the same zip in CI and attaches it to a GitHub Release. Uploading to the
Workshop is still manual; see [WORKSHOP.md](./WORKSHOP.md).

## Models

`assets/beavertails.assets` is a build artifact, committed because rebuilding it needs Blender
and a licensed Unity 2020.3.48f1. The bundle carries geometry only; materials are assigned at
runtime through `KitchenLib.Utils.MaterialUtils`, so PlateUp's shaders never have to exist in
the Unity project.

The Blender sources are not in the repo yet.

## Versioning

`YY.M.revision`, matching every other project here. The month is not zero-padded, because
`26.08.1` is not valid SemVer.

The version is not written down anywhere in the repo. Tagging is the whole release action:
CI passes the tag to the build, and `Mod.cs` reads it back off the assembly. A build with no
tag reports `0.0.0-dev`.

    ./scripts/package.sh          # 0.0.0-dev
    ./scripts/package.sh 26.8.3   # what CI does from a tag

## Contributing

[CONTRIBUTING.md](./CONTRIBUTING.md) covers the DCO sign-off and what a PR needs. If you are
pointing an AI agent at this repo, [AGENTS.md](./AGENTS.md) has the architecture and the traps
that cost real debugging time.

## Licence

AGPL-3.0-only. See [LICENSE](./LICENSE).

Beaver tails are a Canadian pastry. This mod is not affiliated with, endorsed by, or connected
to BeaverTails Canada Inc. or any other company.

## Contact

- Discord: [FireBall Codes](https://discord.gg/QpV82CFfVD)
- Website: [fireball1725.ca](https://fireball1725.ca)
