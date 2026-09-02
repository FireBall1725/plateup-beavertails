// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2026 FireBall1725

using System.Collections.Generic;
using Kitchen;
using KitchenData;
using KitchenLib.References;
using KitchenLib.Utils;
using KitchenMods;
using UnityEngine;

namespace BeaverTails {
  // A custom item defaults to ItemStorageFlags.None, which every prep station and freezer rejects.
  public class TuningSystem : GenericSystemBase, IModSystem {
    private bool applied;

    protected override void OnUpdate() {
      if ( applied || GameData.Main == null ) {
        return;
      }

      applied = true;

      var storable = 0;
      foreach ( var custom in GDOUtils.GetCustomGameDataObjectsFromMod( BeaverTailsMod.Guid )) {
        if ( custom?.GameDataObject is Item item && ShouldStore( item )) {
          Gdo.AllowStorage( item );
          storable++;
        }
      }

      Debug.Log( $"[BeaverTails] tuning applied: {storable} storable items" );

      // Kept in release builds while the rack leak is open, since this is the only view of what the shop filters read.
      foreach ( var custom in GDOUtils.GetCustomGameDataObjectsFromMod( BeaverTailsMod.Guid )) {
        if ( custom?.GameDataObject is Appliance appliance ) {
          DumpAppliance( appliance );
        }
      }

      DumpAppliance( Gdo.Appliance( ApplianceReferences.Hob ));

      // The base game's roux already owns Pot + Butter + Flour, so our gravy starts from
      // its output rather than fighting it for those ingredients.
      foreach ( var id in new[]
      {
                ItemReferences.RouxUncooked,
                ItemReferences.RouxCooked,
                ItemReferences.RouxComplete,
                ItemReferences.RouxPortion,
            } ) {
        DumpItem( id );
      }
    }

    // What the shop filters actually read off an appliance.
    private static void DumpAppliance( Appliance appliance ) {
      if ( appliance == null ) {
        Debug.Log( "[BeaverTails][shop] appliance not found" );
        return;
      }

      Debug.Log( $"[BeaverTails][shop] {appliance.name} ({appliance.ID}) "
                + $"purchasable {appliance.IsPurchasable} "
                + $"asUpgrade {appliance.IsPurchasableAsUpgrade} "
                + $"tags {appliance.ShoppingTags} "
                + $"needsIngredient [{Names( appliance.RequiresIngredientForShop )}] "
                + $"needsProcess [{Names( appliance.RequiresProcessForShop )}] "
                + $"needsAppliance [{Names( appliance.RequiresForShop )}]" );
    }

    private static string Names<T>( List<T> list ) where T : GameDataObject {
      if ( list == null ) {
        return "null";
      }

      var names = new List<string>();
      foreach ( var entry in list ) {
        names.Add( entry == null ? "null" : entry.name );
      }

      return string.Join( ", ", names.ToArray());
    }

    // A base-game side card dumped whole, because Broccoli is the game's own working example
    // of what we are building.
    private static void DumpDish( int dishId, string label ) {
      if ( !( GDOUtils.GetExistingGDO( dishId ) is Dish dish )) {
        Debug.Log( $"[BeaverTails] {label}: not found" );
        return;
      }

      Debug.Log( $"[BeaverTails] {label} type {dish.Type} group {dish.UnlockGroup} "
                + $"difficulty {dish.Difficulty} "
                + $"providesSide {dish.ProvidesPhase( MenuPhase.Side )} "
                + $"providesMain {dish.ProvidesPhase( MenuPhase.Main )}" );

      if ( dish.UnlocksMenuItems != null ) {
        foreach ( var menuItem in dish.UnlocksMenuItems ) {
          Debug.Log( $"[BeaverTails] {label} menu item {menuItem.Item?.name} "
                    + $"phase {menuItem.Phase} weight {menuItem.Weight} "
                    + $"dynamic {menuItem.DynamicMenuType} "
                    + $"on {menuItem.DynamicMenuIngredient?.name}" );

          if ( menuItem.Item != null ) {
            DumpItem( menuItem.Item.ID );
          }
        }
      }

      if ( dish.UnlocksIngredients != null ) {
        foreach ( var unlock in dish.UnlocksIngredients ) {
          Debug.Log( $"[BeaverTails] {label} ingredient unlock {unlock.Ingredient?.name} "
                    + $"on {unlock.MenuItem?.name}" );
        }
      }

      if ( dish.MinimumIngredients != null ) {
        foreach ( var ingredient in dish.MinimumIngredients ) {
          Debug.Log( $"[BeaverTails] {label} needs {ingredient?.name}" );
        }
      }
    }

    // What an item is made of and what can be done to it, printed at startup because it lives
    // in asset data and cannot be read any other way.
    private static void DumpItem( int id ) {
      var item = Gdo.Item( id );
      if ( item == null ) {
        Debug.Log( $"[BeaverTails] item {id}: not found" );
        return;
      }

      var provider = item.DedicatedProvider == null ? "none" : item.DedicatedProvider.name;
      Debug.Log( $"[BeaverTails] item {item.name} ({id}) provider {provider} "
                + $"split {item.SplitCount} of "
                + $"{( item.SplitSubItem == null ? "nothing" : item.SplitSubItem.name )}" );

      // DerivedProcesses and DerivedSets, not Processes and Sets: the latter are
      // KitchenLib's authoring names and the runtime reads the derived ones.
      if ( item.DerivedProcesses != null ) {
        foreach ( var process in item.DerivedProcesses ) {
          var name = process.Process == null ? "null" : process.Process.name;
          var result = process.Result == null ? "none" : process.Result.name;
          Debug.Log( $"[BeaverTails]   {item.name} --{name} {process.Duration}s--> "
                    + $"{result} bad {process.IsBad}" );
        }
      }

      if ( item is ItemGroup group && group.DerivedSets != null ) {
        foreach ( var set in group.DerivedSets ) {
          var parts = new List<string>();
          foreach ( var member in set.Items ) {
            parts.Add( member == null ? "null" : member.name );
          }

          Debug.Log( $"[BeaverTails]   {item.name} set min {set.Min} max {set.Max} "
                    + $"mandatory {set.IsMandatory}: {string.Join( " + ", parts.ToArray())}" );
        }
      }
    }

    // Plates and pots are held by their own appliances; everything else the mod makes is food
    // and should be storable.
    private static bool ShouldStore( Item item ) {
      var name = item.name ?? string.Empty;
      return name.IndexOf( "_plated", System.StringComparison.Ordinal ) < 0
          && name.IndexOf( "pot_with", System.StringComparison.Ordinal ) < 0;
    }
  }
}
