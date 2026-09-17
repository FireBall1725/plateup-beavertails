// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2026 FireBall1725

using Kitchen;
using KitchenData;
using KitchenMods;
using UnityEngine;

namespace BeaverTails {
  // Lets the packs that offer a second base dish keep doing so on a Beaver Tails run.
  //
  // BeaverTailsDish sets BlocksAllOtherFood, which is what keeps vanilla's loose sides and
  // desserts out of the card pool. FilterBasic implements that by dropping every Dish candidate
  // missing from AllowedFoods, and it does not care what kind of dish it is, so it also drops
  // the base dishes. The Autumn card's pack turns on AllowBaseDishes and then finds nothing but
  // our own fifteen cards, which is what a player sees as being forced into Beaver Tail variants
  // with no way to reroll out.
  //
  // Naming the base dishes here restores that one pack and changes nothing else: the normal
  // end-of-day pack filters DishType.Base before it ever reaches the BlocksAllOtherFood check,
  // and sides and desserts are not Base, so they stay blocked.
  //
  // GenericSystemBase, or a joining client never runs it and the host and client disagree about
  // which cards can be offered.
  public class AllowBaseDishesSystem : GenericSystemBase, IModSystem {
    private bool done;

    protected override void OnUpdate() {
      if ( done ) {
        return;
      }

      var data = GameData.Main;
      var dish = Gdo.Own<Dish>( BeaverTailsDish.NameId );
      if ( data == null || dish?.AllowedFoods == null ) {
        return;
      }

      var found = 0;
      var added = 0;

      foreach ( var candidate in data.Get<Dish>()) {
        if ( candidate == null || candidate.Type != DishType.Base ) {
          continue;
        }

        found++;

        if ( dish.AllowedFoods.Contains( candidate )) {
          continue;
        }

        dish.AllowedFoods.Add( candidate );
        added++;
      }

      // Latch on having seen the dishes rather than on having added any, so a second run in the
      // same process finds them already there and still stops scanning.
      if ( found == 0 ) {
        return;
      }

      done = true;
      Debug.Log( $"[BeaverTails] {added} of {found} base dishes added to AllowedFoods, "
                + $"{dish.AllowedFoods.Count} entries total" );
    }
  }
}
