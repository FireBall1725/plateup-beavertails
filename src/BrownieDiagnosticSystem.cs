// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2026 FireBall1725

using System.Linq;
using System.Collections.Generic;
using Kitchen;
using KitchenData;
using KitchenLib.References;
using KitchenMods;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace BeaverTails {
  // Debug only: dumps the base game's Brownies chain at startup, and on F6 reports why a menu is impossible.
  public class BrownieDiagnosticSystem : GenericSystemBase, IModSystem {
    private const KeyCode DumpKey = KeyCode.F6;

    private EntityQuery unlocks;
    private EntityQuery appliances;
    private EntityQuery parcels;
    private bool dumpedRecipe;

    protected override void Initialise() {
      base.Initialise();
      unlocks = GetEntityQuery( typeof( CProgressionUnlock ));
      appliances = GetEntityQuery( new QueryHelper()
          .All( typeof( CAppliance )).None( typeof( CDestroyApplianceAtDay )));
      parcels = GetEntityQuery( typeof( CLetterAppliance ));
    }

    protected override void OnUpdate() {
      if ( !dumpedRecipe && GameData.Main != null ) {
        dumpedRecipe = true;
        DumpPlateSides();
      }

      if ( Input.GetKeyDown( DumpKey ) && GameData.Main != null ) {
        DumpMenuCheck();
      }
    }

    // Measures the base game's Broccoli and Chips sides, because their footprint is the
    // target our poutine should match on a plate.
    private void DumpPlateSides() {
      Log( "================ side size recon ================" );

      Measure( "Broccoli - Serving (base side)", Gdo.Item( ItemReferences.BroccoliServing ));
      Measure( "Chips - Cooked (base side)", Gdo.Item( ItemReferences.ChipsCooked ));
      Measure( "OURS poutine", Gdo.Own<Item>( PoutineItem.NameId ));
      Measure( "OURS double cheese poutine", Gdo.Own<Item>( DoubleCheesePoutineItem.NameId ));
      Measure( "Plate (the surface it stands on)", Gdo.Item( ItemReferences.Plate ));
      Measure( "OURS classic tail (owns the middle)", Gdo.Own<Item>( BeaverTailClassicItem.NameId ));
      Measure( "OURS classic PLATED (whole thing)",
          Gdo.Own<Item>( BeaverTailClassicPlatedItem.NameId ));

      Log( "================ end side size recon ================" );
    }

    // Instantiate and read back, since Renderer.bounds is meaningless on a prefab and Mesh.bounds returns 65536.
    private static void Measure( string label, Item item ) {
      if ( item == null ) {
        Log( $"  {label}: ITEM NULL" );
        return;
      }

      GameObject prefab;
      try {
        prefab = item.Prefab;
      } catch ( System.Exception ) {
        Log( $"  {label}: prefab threw" );
        return;
      }

      if ( prefab == null ) {
        Log( $"  {label}: PREFAB NULL" );
        return;
      }

      GameObject instance = null;
      try {
        instance = Object.Instantiate( prefab );
        instance.transform.position = Vector3.zero;
        instance.transform.rotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;

        var has = false;
        var bounds = new Bounds();
        var counted = 0;

        foreach ( var renderer in instance.GetComponentsInChildren<Renderer>( false )) {
          if ( !renderer.enabled ) {
            continue;
          }

          counted++;
          if ( !has ) {
            bounds = renderer.bounds;
            has = true;
            continue;
          }

          bounds.Encapsulate( renderer.bounds );
        }

        if ( !has ) {
          Log( $"  {label} = {Short( item.name )}: no enabled renderers" );
          return;
        }

        var size = bounds.size;
        Log( $"  {label} = {Short( item.name )}: "
            + $"size ({size.x:F3}, {size.y:F3}, {size.z:F3}) "
            + $"footprint {Mathf.Max( size.x, size.z ):F3} "
            + $"renderers {counted}" );
      } finally {
        if ( instance != null ) {
          Object.Destroy( instance );
        }
      }
    }

    private static void DumpPrefab( string label, Item item ) {
      if ( item == null ) {
        Log( $"  {label}: ITEM NULL" );
        return;
      }

      var prefab = item.Prefab;
      if ( prefab == null ) {
        Log( $"  {label} = {Short( item.name )} ({item.ID}): PREFAB NULL" );
        return;
      }

      Log( $"  {label} = {Short( item.name )} ({item.ID}) mergeableSide {item.IsMergeableSide}"
          + ( item is ItemGroup g ? $" canContainSide {g.CanContainSide}" : "" ));

      // Immediate children only: a side has to hang off one of these.
      var children = new List<string>();
      for ( var i = 0; i < prefab.transform.childCount; i++ ) {
        var child = prefab.transform.GetChild( i );
        children.Add( $"{child.name}{( child.gameObject.activeSelf ? "" : " (off)" )}" );
      }

      Log( $"    children ({children.Count}): "
          + ( children.Count == 0 ? "none" : string.Join( ", ", children.ToArray())));

      var view = prefab.GetComponent<ItemGroupView>();
      if ( view == null ) {
        Log( "    ItemGroupView: NONE" );
        return;
      }

      if ( view.ComponentGroups == null || view.ComponentGroups.Count == 0 ) {
        Log( "    ItemGroupView: present, ComponentGroups EMPTY" );
        return;
      }

      Log( $"    ItemGroupView: {view.ComponentGroups.Count} mapping(s)" );
      foreach ( var group in view.ComponentGroups ) {
        var mapped = group.Item == null ? "NULL item" : Short( group.Item.name );
        var obj = group.GameObject == null ? "no GameObject" : group.GameObject.name;
        var extra = group.Objects == null ? 0 : group.Objects.Count;
        Log( $"      {mapped} -> {obj}"
            + ( extra > 0 ? $" (+{extra} in Objects)" : "" )
            + $" drawAll {group.DrawAll}" );
      }
    }

    private static void DumpDish( string label, int id ) {
      if ( !( GDOUtilsSafe( id ) is Dish dish )) {
        Log( $"dish {label}: NOT FOUND" );
        return;
      }

      Log( $"dish {label} = {dish.name} ({dish.ID}) type {dish.Type} group {dish.UnlockGroup}" );

      Log( "  RequiredProcesses:" );
      if ( dish.RequiredProcesses != null ) {
        foreach ( var process in dish.RequiredProcesses ) {
          if ( process == null ) {
            continue;
          }

          var enabler = process.BasicEnablingAppliance;
          Log( $"    {process.name} ({process.ID}) x{process.EnablingApplianceCount} "
              + $"grants {( enabler == null ? "nothing" : enabler.name )}" );
        }
      }

      Log( "  MinimumIngredients:" );
      if ( dish.MinimumIngredients != null ) {
        foreach ( var ingredient in dish.MinimumIngredients ) {
          if ( ingredient == null ) {
            continue;
          }

          var provider = ingredient.DedicatedProvider;
          Log( $"    {ingredient.name} ({ingredient.ID}) "
              + $"provider {( provider == null ? "DEFAULT" : provider.name )}" );
        }
      }

      Log( "  UnlocksMenuItems:" );
      if ( dish.UnlocksMenuItems != null ) {
        foreach ( var menuItem in dish.UnlocksMenuItems ) {
          Log( $"    {( menuItem.Item == null ? "null" : menuItem.Item.name )} "
              + $"phase {menuItem.Phase} weight {menuItem.Weight}" );
        }
      }
    }

    private static void DumpProcess( string label, int id ) {
      if ( !( GDOUtilsSafe( id ) is Process process )) {
        Log( $"process {label}: NOT FOUND" );
        return;
      }

      var enabler = process.BasicEnablingAppliance;
      Log( $"process {label} = {process.name} ({process.ID}) x{process.EnablingApplianceCount} "
          + $"grants {( enabler == null ? "NOTHING" : enabler.name )} "
          + $"pseudoFor {( process.IsPseudoprocessFor == null ? "none" : process.IsPseudoprocessFor.name )}" );
    }

    private static void DumpItem( string label, Item item ) {
      if ( item == null ) {
        Log( $"  {label}: NOT FOUND" );
        return;
      }

      var provider = item.DedicatedProvider;
      Log( $"  {label} = {item.name} ({item.ID}) type {item.GetType().Name} "
          + $"provider {( provider == null ? "none" : provider.name )} "
          + $"split {item.SplitCount} of {( item.SplitSubItem == null ? "nothing" : item.SplitSubItem.name )}" );

      if ( item.DerivedProcesses != null ) {
        foreach ( var process in item.DerivedProcesses ) {
          var name = process.Process == null ? "null" : process.Process.name;
          var result = process.Result == null ? "none" : process.Result.name;
          Log( $"    --{name} {process.Duration}s--> {result} bad {process.IsBad}" );
        }
      }

      // What the container turns into once emptied, so the kitchen knows where to put it.
      if ( item.SplitDepletedItems != null && item.SplitDepletedItems.Count > 0 ) {
        var parts = new List<string>();
        foreach ( var depleted in item.SplitDepletedItems ) {
          parts.Add( depleted == null ? "null" : depleted.name );
        }

        Log( $"    depletes to: {string.Join( " + ", parts.ToArray())}" );
      }

      if ( item is ItemGroup group && group.DerivedSets != null ) {
        foreach ( var set in group.DerivedSets ) {
          var parts = new List<string>();
          foreach ( var member in set.Items ) {
            parts.Add( member == null ? "null" : member.name );
          }

          Log( $"    set min {set.Min} max {set.Max} mandatory {set.IsMandatory}: "
              + string.Join( " + ", parts.ToArray()));
        }
      }
    }

    private static GameDataObject GDOUtilsSafe( int id ) {
      return GameData.Main.TryGet<GameDataObject>( id, out var gdo ) ? gdo : null;
    }

    private void DumpMenuCheck() {
      Log( "================ menu diagnostic ================" );

      var required = new HashSet<Process>();
      using ( var held = unlocks.ToEntityArray( Allocator.Temp )) {
        Log( $"held unlocks: {held.Length}" );
        foreach ( var entity in held ) {
          var id = EntityManager.GetComponentData<CProgressionUnlock>( entity ).ID;
          if ( !GameData.Main.TryGet<Dish>( id, out var dish )) {
            continue;
          }

          var names = new List<string>();
          foreach ( var process in dish.RequiredProcesses ) {
            var target = process.IsPseudoprocessFor ?? process;
            required.Add( target );
            names.Add( Short( target.name ));
          }

          Log( $"  card {Short( dish.name )} requires: "
              + ( names.Count == 0 ? "nothing" : string.Join( ", ", names.ToArray())));
        }
      }

      var satisfied = new Dictionary<Process, string>();
      StrikeOff( required, satisfied, placed: true );
      StrikeOff( required, satisfied, placed: false );

      foreach ( var pair in satisfied ) {
        Log( $"  satisfied {Short( pair.Key.name )} by {pair.Value}" );
      }

      if ( required.Count == 0 ) {
        Log( "RESULT: every required process is available, menu is possible" );
      } else {
        foreach ( var process in required ) {
          var enabler = process.BasicEnablingAppliance;
          Log( $"RESULT: MISSING {Short( process.name )} ({process.ID}), "
              + $"grants {( enabler == null ? "NOTHING - dish can never be satisfied" : Short( enabler.name ))}" );
        }
      }

      Log( "================ end menu diagnostic ================" );
    }

    private void StrikeOff( HashSet<Process> required, Dictionary<Process, string> satisfied, bool placed ) {
      var ids = new List<int>();
      if ( placed ) {
        using ( var array = appliances.ToComponentDataArray<CAppliance>( Allocator.Temp )) {
          foreach ( var entry in array ) {
            ids.Add( entry.ID );
          }
        }
      } else {
        using ( var array = parcels.ToComponentDataArray<CLetterAppliance>( Allocator.Temp )) {
          foreach ( var entry in array ) {
            ids.Add( entry.ApplianceID );
          }
        }
      }

      var seen = new HashSet<int>();
      var names = new List<string>();
      var kind = placed ? "in kitchen" : "in parcel";

      foreach ( var id in ids ) {
        if ( id == 0 || !seen.Add( id ) || !GameData.Main.TryGet<Appliance>( id, out var appliance )) {
          continue;
        }

        // Names, not a count: a count cannot tell a non-matching appliance from one that
        // was never delivered.
        names.Add( Short( appliance.name ));

        if ( appliance.Processes == null ) {
          continue;
        }

        foreach ( var entry in appliance.Processes ) {
          if ( entry.Validity == ProcessValidity.DoesNotRegister || entry.Process == null ) {
            continue;
          }

          if ( required.Remove( entry.Process )) {
            satisfied[entry.Process] = $"{Short( appliance.name )} ({kind})";
          }
        }
      }

      names.Sort();
      Log( $"appliances {kind}: {seen.Count} distinct" );
      foreach ( var name in names ) {
        Log( $"    {kind}: {name}" );
      }
    }

    // Our own GDO names carry the mod GUID, which makes every line unreadable.
    private static string Short( string name ) {
      if ( string.IsNullOrEmpty( name )) {
        return "unnamed";
      }

      var marker = name.IndexOf( " - ", System.StringComparison.Ordinal );
      return name.StartsWith( BeaverTailsMod.Guid, System.StringComparison.Ordinal ) && marker >= 0
          ? "OURS:" + name.Substring( marker + 3 )
          : name;
    }

    private static void Log( string message ) {
      Debug.Log( "[BeaverTails][diag] " + message );
    }
  }
}
