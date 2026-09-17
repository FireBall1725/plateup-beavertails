// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2026 FireBall1725

using System;
using System.Collections.Generic;
using HarmonyLib;
using Kitchen;
using KitchenData;
using KitchenLib.References;
using Unity.Entities;
using UnityEngine;

namespace BeaverTails {
  // Two merges the mod has to refuse, both of which the game would otherwise allow silently.
  //
  // A lemon slice added to a finished plate vanished. `ItemGroup.CanContainSide` is on for every
  // plated recipe so a poutine or a side of chips can ride along, but `IsGroupSatisfied` treats
  // that slot as "any one leftover item where IsMergeableSide", with no allow-list. The vanilla
  // sliced lemon qualifies, so the plate swallowed it, drew nothing, and `IsRequestSatisfied`
  // still reported the dish as correct. It cannot instead become a Killaloe: an ItemList is flat,
  // so plate-plus-classic-plus-lemon and plate-plus-killaloe are different shapes, and teaching
  // the Killaloe plate to accept both makes a plain plated Classic ambiguous with it.
  //
  // A dusting added to a tail that already carries it made a Classic out of two cinnamons. The
  // Classic's second set names both dustings so either can go on first, and a set cannot know
  // which one the tail in the other set already used.
  //
  // Named refusals rather than an "only these may be sides" allow-list, because the slot is a
  // base-game mechanic our own poutine and chips sides ride, so an allow-list would have to
  // enumerate them and stay correct as sides are added. It would NOT have broken the maple syrup
  // card: that goes through AcceptIntoExtraSatisfaction, which returns early on
  // TransferFlags.RequireMerge and matches the bottle against CWaitingForItem.Extra. The syrup
  // never touches the plate; the customer asks mid-meal and you carry the bottle to the table.
  //
  // Separate from BrownieMergePatch, which patches the same method, so a binding failure in one
  // costs one recipe and says so in the log rather than taking both down.
  internal static class PlatedMergePatch {
    private static readonly HashSet<int> Plated = new HashSet<int>();
    private static readonly Dictionary<int, int> AlreadyDusted = new Dictionary<int, int>();
    private static bool resolved;
    private static bool announced;

    // The four other AttemptItemMerge overloads all call this one.
    private static readonly Type[] Signature = {
      typeof( EntityContext ),
      typeof( Entity ).MakeByRefType(),
      typeof( int ),
      typeof( int ),
      typeof( ItemList ),
      typeof( ItemList ),
      typeof( MergeCondition ),
      typeof( MergeCondition ),
      typeof( bool ),
    };

    internal static void Apply() {
      try {
        var target = AccessTools.Method(
            typeof( ItemExtensions ), nameof( ItemExtensions.AttemptItemMerge ), Signature );
        if ( target == null ) {
          Debug.LogError(
              "[BeaverTails] no AttemptItemMerge matching our signature; plates will swallow a lemon" );
          return;
        }

        new Harmony( BeaverTailsMod.Guid + ".plated" ).Patch(
            target,
            prefix: new HarmonyMethod( typeof( PlatedMergePatch ), nameof( Prefix )));
        Debug.Log( "[BeaverTails] plated merge patch applied" );
      } catch ( Exception e ) {
        Debug.LogError( $"[BeaverTails] plated merge patch failed, a lemon will vanish into a plate: {e}" );
      }
    }

    // Resolved on first merge, not at Apply: KitchenLib has not converted our objects yet. One
    // shot is enough, because the first merge of a run is long after registration, and retrying
    // would put these lookups on every merge in the game.
    private static bool Resolve() {
      if ( resolved ) {
        return Plated.Count > 0;
      }

      resolved = true;

      foreach ( var nameId in PlatedRecipeItem.AllNameIds ) {
        var plate = Gdo.Own<Item>( nameId );
        if ( plate != null ) {
          Plated.Add( plate.ID );
        }
      }

      Pair( BeaverTailCinnamonItem.NameId, Gdo.Lib( Gdo.LibKeys.Cinnamon ));
      Pair( BeaverTailSugarItem.NameId, Gdo.Item( ItemReferences.Sugar ));

      Debug.Log( $"[BeaverTails] plated merge patch resolved {Plated.Count}"
                + $" of {PlatedRecipeItem.AllNameIds.Length} plates"
                + $" and {AlreadyDusted.Count} of 2 dusted tails" );

      return Plated.Count > 0;
    }

    private static void Pair( string tailNameId, Item dusting ) {
      var tail = Gdo.Own<Item>( tailNameId );
      if ( tail != null && dusting != null ) {
        AlreadyDusted[tail.ID] = dusting.ID;
      }
    }

    private static void Prefix( int item1_id, int item2_id, ref MergeCondition c1, ref MergeCondition c2 ) {
      if ( !Resolve()) {
        return;
      }

      if ( !Refuse( item1_id, item2_id ) && !Refuse( item2_id, item1_id )) {
        return;
      }

      c1 = MergeCondition.NoMerge;
      c2 = MergeCondition.NoMerge;

      if ( !announced ) {
        announced = true;
        Debug.Log( "[BeaverTails] refused a merge onto a finished plate or an already dusted tail" );
      }
    }

    private static bool Refuse( int host, int incoming ) {
      if ( incoming == ItemReferences.LemonSliced && Plated.Contains( host )) {
        return true;
      }

      return AlreadyDusted.TryGetValue( host, out var dusting ) && incoming == dusting;
    }
  }
}
