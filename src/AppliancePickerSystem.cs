// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2026 FireBall1725

using System;
using System.Collections.Generic;
using Kitchen;
using KitchenData;
using KitchenLib.References;
using KitchenLib.Utils;
using KitchenMods;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace BeaverTails {
  // F4 spawns an appliance in front of the player via CCreateAppliance, which is the game's own
  // spawn path, so what arrives is real rather than a prop.
  public class AppliancePickerSystem : GameSystemBase, IModSystem {
    private const KeyCode ToggleKey = KeyCode.F4;

    // The base-game appliances this mod's recipes reach for, deliberately not every appliance
    // in the game.
    private static readonly (string Label, int Id)[] Storage =
    {
            ("Frozen Prep Station", ApplianceReferences.FrozenPrepStation),
            ("Prep Station", ApplianceReferences.PrepStation),
            ("Freezer", ApplianceReferences.Freezer),
            ("Oven", ApplianceReferences.Oven),
            ("Hob", ApplianceReferences.Hob),
            ("Source - Chocolate", ApplianceReferences.SourceChocolate),
            ("Source - Chocolate Syrup", ApplianceReferences.SourceChocolateSyrup),
            ("Source - Strawberry Syrup", ApplianceReferences.SourceStrawberrySyrup),
            ("Source - Flour", ApplianceReferences.SourceFlour),
            ("Source - Sugar", ApplianceReferences.SourceSugar),
            ("Source - Milk", ApplianceReferences.SourceMilk),
            ("Source - Meat", ApplianceReferences.SourceMeat),
            ("Source - Butter", ApplianceReferences.SourceButter),
            ("Source - Potato", ApplianceReferences.SourcePotato),
        };

    // IngredientLib's dispensers, looked up by name because another mod's GDOs have no
    // compile-time constants.
    private static readonly string[] LibProviders =
        { Gdo.LibKeys.Honey, Gdo.LibKeys.Crackers, Gdo.LibKeys.Vinegar };

    private static AppliancePickerOverlay overlay;
    private EntityQuery players;

    protected override void Initialise() {
      base.Initialise();
      players = GetEntityQuery( typeof( CPlayer ), typeof( CPosition ));
    }

    protected override void OnUpdate() {
      if ( overlay == null ) {
        var host = new GameObject( "BeaverTailsAppliancePicker" );
        UnityEngine.Object.DontDestroyOnLoad( host );
        overlay = host.AddComponent<AppliancePickerOverlay>();
        overlay.Owner = this;
        Debug.Log( $"[BeaverTails] appliance spawner ready, {ToggleKey} to open" );
      }

      if ( Input.GetKeyDown( ToggleKey )) {
        overlay.Toggle();
      }
    }

    internal bool IsHost() => Session.HostIdentifier == 0;

    internal List<ApplianceEntry> BuildAppliances( string filter ) {
      var results = new List<ApplianceEntry>();
      if ( GameData.Main == null ) {
        return results;
      }

      var needle = string.IsNullOrEmpty( filter ) ? null : filter.ToLowerInvariant();

      foreach ( var (label, id) in Storage ) {
        Add( results, needle, id, label, "base" );
      }

      foreach ( var key in LibProviders ) {
        var provider = Gdo.LibProvider( key );
        if ( provider != null ) {
          Add( results, needle, provider.ID, $"Source - {key}", "lib" );
        }
      }

      foreach ( var custom in GDOUtils.GetCustomGameDataObjectsFromMod( BeaverTailsMod.Guid )) {
        if ( custom?.GameDataObject is Appliance appliance ) {
          Add( results, needle, appliance.ID, DisplayName( appliance ), "ours" );
        }
      }

      results.Sort(( a, b ) => {
        if ( a.Source != b.Source ) {
          return string.Compare( a.Source, b.Source, StringComparison.Ordinal );
        }

        return string.Compare( a.Name, b.Name, StringComparison.OrdinalIgnoreCase );
      } );

      return results;
    }

    private static void Add(
        List<ApplianceEntry> into, string needle, int id, string name, string source ) {
      if ( id == 0 || !GameData.Main.TryGet<Appliance>( id, out _ )) {
        return;
      }

      if ( needle != null && name.ToLowerInvariant().IndexOf( needle, StringComparison.Ordinal ) < 0 ) {
        return;
      }

      into.Add( new ApplianceEntry { Id = id, Name = name, Source = source } );
    }

    // KitchenLib names custom GDOs "<guid> - <uniqueNameId>", unreadable in a list.
    private static string DisplayName( Appliance appliance ) {
      var info = appliance.Info?.Get( Locale.English );
      if ( info != null && !string.IsNullOrEmpty( info.Name )) {
        return info.Name;
      }

      var raw = appliance.name ?? appliance.ID.ToString();
      var split = raw.LastIndexOf( " - ", StringComparison.Ordinal );
      return split >= 0 ? raw.Substring( split + 3 ) : raw;
    }

    internal string Spawn( int id, string label ) {
      if ( !IsHost()) {
        return "not the host, refusing to spawn";
      }

      if ( !PlayerPosition( out var position )) {
        return "no player to spawn next to";
      }

      // One tile in front so it does not land underfoot; CPosition.Rotation has no
      // operator* for Vector3.
      var facing = ( Quaternion )position.Rotation * Vector3.forward;
      var target = position;
      target.Position = new Unity.Mathematics.float3(
          target.Position.x + facing.x, target.Position.y, target.Position.z + facing.z );

      var entity = EntityManager.CreateEntity( typeof( CCreateAppliance ), typeof( CPosition ));
      EntityManager.SetComponentData( entity, new CCreateAppliance { ID = id } );
      EntityManager.SetComponentData( entity, target );

      Debug.Log( $"[BeaverTails] spawned '{label}' ({id})" );
      return $"spawned '{label}'";
    }

    private bool PlayerPosition( out CPosition position ) {
      position = default;
      using ( var found = players.ToComponentDataArray<CPosition>( Allocator.Temp )) {
        if ( found.Length == 0 ) {
          return false;
        }

        position = found[0];
        return true;
      }
    }
  }

  internal struct ApplianceEntry {
    public int Id;
    public string Name;
    public string Source;
  }
}
