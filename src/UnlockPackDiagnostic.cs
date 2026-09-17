// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2026 FireBall1725

using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using Kitchen;
using KitchenData;
using KitchenMods;
using UnityEngine;

namespace BeaverTails {
  // Debug only: says which unlock pack produced each pair of cards, and what it had to choose from.
  //
  // The packs themselves are Odin-serialised inside the game's assets, so which filters and
  // sorters a pack like the Autumn card's carries cannot be read from the assemblies. One run
  // with this on answers it.
  //
  // The protected overload is the one worth watching: it is handed the candidates AFTER
  // ShouldBlockCard and SortCards have run, so the list says whether a card was filtered out or
  // merely sorted behind ours. GetOffsetChoice indexes that list by the reroll count, and rerolls
  // run under a FixedSeedContext, so the order is the same every time and only the index moves.
  //
  // Attaches itself rather than being called from Mod.cs, because the whole file is removed from
  // a release build and a call site there would not compile.
  public class UnlockPackDiagnostic : GenericSystemBase, IModSystem {
    private static bool attached;

    // ModularUnlockPack declares GetOptions twice, so the name alone is an ambiguous match.
    private static readonly Type[] Signature = {
      typeof( List<Unlock> ),
      typeof( HashSet<int> ),
      typeof( UnlockRequest ),
      typeof( int ),
    };

    // Enough to see past our own fifteen cards, short enough to read in a log.
    private const int Listed = 20;

    protected override void OnUpdate() {
      if ( attached ) {
        return;
      }

      attached = true;

      // Hand-patched rather than PatchAll for the reason given in BrownieMergePatch: a binding
      // failure here must cost a log line, not the whole mod.
      try {
        var target = AccessTools.Method( typeof( ModularUnlockPack ), "GetOptions", Signature );
        if ( target == null ) {
          Debug.LogWarning( "[BeaverTails] no ModularUnlockPack.GetOptions matching our signature" );
          return;
        }

        new Harmony( BeaverTailsMod.Guid + ".packs" ).Patch(
            target,
            postfix: new HarmonyMethod( typeof( UnlockPackDiagnostic ), nameof( Postfix )));
        Debug.Log( "[BeaverTails] unlock pack diagnostic attached" );
      } catch ( Exception e ) {
        Debug.LogError( $"[BeaverTails] unlock pack diagnostic failed to attach: {e}" );
      }
    }

    private static void Postfix(
        ModularUnlockPack __instance, List<Unlock> candidates, int skip_choices, UnlockOptions __result ) {
      var ours = 0;
      var bases = 0;

      foreach ( var candidate in candidates ) {
        if ( candidate is Dish { Type: DishType.Base } ) {
          bases++;
        }

        if ( candidate != null && OurIds().Contains( candidate.ID )) {
          ours++;
        }
      }

      Debug.Log( $"[BeaverTails][pack] '{__instance?.name}' skip {skip_choices}: "
                + $"{candidates.Count} candidates, {bases} base dishes, {ours} ours. "
                + $"Offered {Describe( __result.Unlock1 )} and {Describe( __result.Unlock2 )}" );

      // In sorted order, because that order plus the reroll index is what decides the offer.
      var head = new StringBuilder();
      for ( var i = 0; i < candidates.Count && i < Listed; i++ ) {
        head.Append( i == 0 ? "" : ", " ).Append( Describe( candidates[i] ));
      }

      Debug.Log( $"[BeaverTails][pack] first {Listed}: {head}" );
    }

    private static HashSet<int> ourIds;

    private static HashSet<int> OurIds() {
      if ( ourIds != null ) {
        return ourIds;
      }

      ourIds = new HashSet<int>();
      foreach ( var gdo in KitchenLib.Utils.GDOUtils.GetCustomGameDataObjectsFromMod( BeaverTailsMod.Guid )) {
        if ( gdo?.GameDataObject != null ) {
          ourIds.Add( gdo.GameDataObject.ID );
        }
      }

      return ourIds;
    }

    private static string Describe( Unlock unlock ) {
      if ( unlock == null ) {
        return "nothing";
      }

      var kind = unlock is Dish dish ? dish.Type.ToString() : unlock.GetType().Name;
      return $"{unlock.Name} ({kind})";
    }
  }
}
