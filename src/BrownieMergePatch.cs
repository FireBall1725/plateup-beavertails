// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2026 FireBall1725

using System;
using HarmonyLib;
using Kitchen;
using KitchenData;
using KitchenLib.References;
using Unity.Entities;
using UnityEngine;

namespace BeaverTails {
  // Lets a vanilla Brownie merge onto a hazelnut-coated tail, and nothing else.
  //
  // The base game gives Brownie a CPreventItemMerge of NoMerge, which blocks BrWOWnie. Clearing
  // that property outright also let a brownie tray wrap a brownie, which resolves to the raw
  // dough tray and filled empty trays with dough for anyone running this mod.
  //
  // No MergeCondition fixes it: AttemptItemMerge reaches the tray through
  // SingleWrapperMergeResult, whose branch needs only CanComp on the brownie side, and that is
  // the same flag the BrWOWnie merge needs.
  //
  // Patched by hand rather than through [HarmonyPatch], because PatchAll makes a binding
  // failure fatal: a wrong signature here already took the whole mod down with "Undefined
  // target method" and a screen telling players to check their dependencies. A miss now costs
  // one recipe and says so in the log.
  internal static class BrownieMergePatch {
    private static int coated;
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
              "[BeaverTails] no AttemptItemMerge matching our signature; BrWOWnie cannot be assembled" );
          return;
        }

        new Harmony( BeaverTailsMod.Guid ).Patch(
            target,
            prefix: new HarmonyMethod( typeof( BrownieMergePatch ), nameof( Prefix )));
        Debug.Log( "[BeaverTails] brownie merge patch applied" );
      } catch ( Exception e ) {
        Debug.LogError( $"[BeaverTails] brownie merge patch failed, BrWOWnie will not assemble: {e}" );
      }
    }

    private static void Prefix( int item1_id, int item2_id, ref MergeCondition c1, ref MergeCondition c2 ) {
      if ( coated == 0 ) {
        coated = Gdo.Own<Item>( BeaverTailHazelnutCoatedItem.NameId )?.ID ?? 0;
        if ( coated == 0 ) {
          return;
        }
      }

      if ( item1_id == ItemReferences.Brownie && item2_id == coated ) {
        c1 = MergeCondition.All;
      } else if ( item2_id == ItemReferences.Brownie && item1_id == coated ) {
        c2 = MergeCondition.All;
      } else {
        return;
      }

      if ( !announced ) {
        announced = true;
        Debug.Log( "[BeaverTails] brownie merge allowed onto a coated tail" );
      }
    }
  }
}
