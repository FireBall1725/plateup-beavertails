# Steam Workshop listing

Copy the description into the Workshop page. Steam BBCode, not Markdown.

## Title

**Beaver Tails**

## Description

```
Canadian fried dough, served as a main.

The base dish is a beaver tail: flour and milk into dough, knead it flat, deep fry it,
then cinnamon and sugar. Everything else is an unlock card you pick up during a run.
Each card is a full recipe with its own ingredients and steps, not a topping you add at
the end.

15 cards in total. Killaloe Sunrise, Hazel Amour, Bananarama, Apple Pie, Triple Trip,
Avalanche, mEHple, Pistach-OH, Strawberry Cheesecake, Coco Vanil and BrWOWnie build on
the tail itself. Fries, Poutine and Double Cheese Poutine are sides. Maple Syrup is a
bottle customers ask for once they are already eating.

[b]Requires KitchenLib, Fryer Appliance and IngredientLib.[/b] Without the fryer you get
a menu you cannot cook, and without IngredientLib several recipes are missing ingredients.

Run into a bug, or want to ask something? Let me know.

[b]Discord:[/b] [url=https://discord.gg/QpV82CFfVD]FireBall Codes[/url]
[b]Website:[/b] [url=https://fireball1725.ca]fireball1725.ca[/url]
```

The requirements line is belt and braces with the Workshop **required items** field. Keep
one of the two; dropping both is what leaves a subscriber staring at a menu they cannot
cook.

Steam is BBCode, not Markdown, so links are `[url=...]text[/url]`. A bare URL auto-links,
but YouTube, Steam store and Workshop URLs expand into a widget instead. Any off-Steam link
sends the visitor through a "you are about to leave Steam" interstitial, which is why both
links are labelled rather than pasted raw: the reader should know where they agreed to go
before the warning page asks them.

Site link is the apex, `https://fireball1725.ca`, not `www.`. Both return 200, so this is
tidiness rather than a redirect.

The invite is permanent (`expires_at` is null, re-checked 2026-08-16). A default Discord
invite lapses after 7 days, so re-check before publishing if it was ever regenerated:

    curl -s "https://discord.com/api/v10/invites/QpV82CFfVD?with_counts=true"

Card names are copied from `src/RecipeCards.cs` and `src/Items/MapleSyrup.cs`. The odd
capitalisation is deliberate and matches the card in game: `mEHple`, `Pistach-OH`,
`BrWOWnie`. The count of 15 is the number of `AddGameDataObject<...Card>()` calls in
`src/Mod.cs`; update it when that list changes.

There is no recipe called "The Classic". The base dish is `Beaver Tails` and the plated
item is `Beaver Tail - Cinnamon Sugar`. Do not name it something a player will never see.

## Shorter still, if you want it

```
Canadian fried dough, served as a main. 15 unlock cards, each a full recipe with its own
ingredients and steps rather than a topping.

Requires KitchenLib, Fryer Appliance and IngredientLib.
```

## Change note for 26.8.0

```
Fixes recipes showing up blank for anyone who joined a multiplayer game instead of
hosting it. Removes the F3, F4 and F5 development hotkeys.
```

## Uploading

The uploader ships `Mods/BeaverTails/content` and nothing else, so the DLL and the asset
bundle go in there and `plateup_mod_metadata.json` stays in the parent. The Release zip is
already laid out that way: extract it over `Mods/` and point ModUploader's Update tab at the
`BeaverTails` folder. An empty
`content` folder fails with "a generic failure from the steamworks API" while title-only
updates keep working, which is a confusing way to find out. `ModsDir` in the csproj
already deploys to the right place.

Do not leave a second copy of the DLL in the parent folder. The loader reads both levels
and you get a duplicate mod GUID.

## Before publishing

- Tag the release and let CI build it: `git tag 26.8.3 && git push origin 26.8.3`. The tag is
  the version, so there is nothing to bump first. Download the zip from the Release rather
  than building by hand; a local build reports `0.0.0-dev` and is not fit to upload.
- If you do build locally, pass the version and turn the dev tools off:
  `./scripts/package.sh 26.8.3`. It refuses to produce a zip that still contains the F3, F4
  and F5 systems, so an accidental debug upload fails loudly instead of shipping hotkeys.
- Add Fryer Appliance (`3765792217`) and IngredientLib (`2913877103`) to the Workshop
  **required items** field. A subscriber without the fryer gets an impossible menu and no
  explanation; a subscriber without IngredientLib gets recipes with holes in them.
- Preview image: `art/workshop_preview.png`, 1280x720, 631 KB. Steam's cap is 1 MB, and
  going over it fails with the same generic Steamworks error as an empty content folder.
