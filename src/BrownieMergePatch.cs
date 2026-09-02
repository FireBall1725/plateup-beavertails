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
  [HarmonyPatch(
      typeof( ItemExtensions ),
      nameof( ItemExtensions.AttemptItemMerge ),
      new[]
      {
                typeof( EntityContext ),
                typeof( Entity ),
                typeof( int ),
                typeof( int ),
                typeof( ItemList ),
                typeof( ItemList ),
                typeof( MergeCondition ),
                typeof( MergeCondition ),
                typeof( bool ),
      },
      new[]
      {
                ArgumentType.Normal,
                ArgumentType.Out,
                ArgumentType.Normal,
                ArgumentType.Normal,
                ArgumentType.Normal,
                ArgumentType.Normal,
                ArgumentType.Ref,
                ArgumentType.Ref,
                ArgumentType.Normal,
      } )]
  internal static class BrownieMergePatch {
    private static int coated;
    private static bool announced;

    // The four other overloads all call this one, so patching it covers every entry point.
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
