// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2026 FireBall1725

using System;
using HarmonyLib;
using Kitchen;
using KitchenData;
using KitchenMods;
using UnityEngine;

namespace BeaverTails {
  // Debug only: says which unlock pack produced each pair of cards, and what it offered.
  //
  // The packs themselves are Odin-serialised inside the game's assets, so which filters and
  // sorters a pack like the Autumn card's carries cannot be read from the assemblies. One run
  // with this on answers it: the pack name plus the options at each reroll index says whether a
  // card is being filtered out or merely sorted behind ours.
  //
  // Attaches itself rather than being called from Mod.cs, because the whole file is removed from
  // a release build and a call site there would not compile.
  public class UnlockPackDiagnostic : GenericSystemBase, IModSystem {
    private static bool attached;

    protected override void OnUpdate() {
      if ( attached ) {
        return;
      }

      attached = true;

      // Hand-patched rather than PatchAll for the reason given in BrownieMergePatch: a binding
      // failure here must cost a log line, not the whole mod.
      try {
        var target = AccessTools.Method( typeof( ModularUnlockPack ), nameof( UnlockPack.GetOptions ));
        if ( target == null ) {
          Debug.LogWarning( "[BeaverTails] no ModularUnlockPack.GetOptions to watch" );
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

    private static void Postfix( ModularUnlockPack __instance, int skip_choices, UnlockOptions __result ) {
      Debug.Log( $"[BeaverTails] pack '{__instance?.name}' skip {skip_choices} offered "
                + $"{Describe( __result.Unlock1 )} and {Describe( __result.Unlock2 )}" );
    }

    private static string Describe( Unlock unlock ) {
      if ( unlock == null ) {
        return "nothing";
      }

      var kind = unlock is Dish dish ? dish.Type.ToString() : unlock.GetType().Name;
      return $"{unlock.Name} ({kind}, {unlock.ID})";
    }
  }
}
