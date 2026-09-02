// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2026 FireBall1725

using System;
using System.Collections.Generic;
using System.Text;
using Kitchen;
using KitchenData;
using KitchenMods;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace BeaverTails {
  // An in-game card picker on IMGUI rather than PlateUp's menu system, granting cards by
  // creating a CProgressionOption + Selected entity the way a real card click does.
  public class CardPickerSystem : GameSystemBase, IModSystem {
    private const KeyCode ToggleKey = KeyCode.F3;

    private static CardPickerOverlay overlay;
    private EntityQuery held;
    private EntityQuery menuItems;
    private EntityQuery available;
    private EntityQuery possibleExtras;

    protected override void Initialise() {
      base.Initialise();
      held = GetEntityQuery( typeof( CProgressionUnlock ));
      menuItems = GetEntityQuery( typeof( CMenuItem ));
      available = GetEntityQuery( typeof( CAvailableIngredient ));
      possibleExtras = GetEntityQuery( typeof( CPossibleExtra ));
    }

    protected override void OnUpdate() {
      if ( overlay == null ) {
        var host = new GameObject( "BeaverTailsCardPicker" );
        UnityEngine.Object.DontDestroyOnLoad( host );
        overlay = host.AddComponent<CardPickerOverlay>();
        overlay.Owner = this;
        Debug.Log( $"[BeaverTails] card picker ready, {ToggleKey} to open" );
      }

      if ( Input.GetKeyDown( ToggleKey )) {
        overlay.Toggle();
      }
    }

    // Every unlock the game knows about, ours and everyone else's.
    internal List<CardEntry> BuildCards( string filter, bool oursOnly ) {
      var results = new List<CardEntry>();
      if ( GameData.Main == null ) {
        return results;
      }

      var heldIds = HeldIds();
      var needle = string.IsNullOrEmpty( filter ) ? null : filter.ToLowerInvariant();

      foreach ( var unlock in GameData.Main.Get<Unlock>()) {
        if ( unlock == null ) {
          continue;
        }

        var ours = unlock.name != null && unlock.name.StartsWith( BeaverTailsMod.Guid );
        if ( oursOnly && !ours ) {
          continue;
        }

        var label = DisplayName( unlock );
        if ( needle != null && label.ToLowerInvariant().IndexOf( needle, StringComparison.Ordinal ) < 0 ) {
          continue;
        }

        results.Add( new CardEntry {
          Id = unlock.ID,
          Name = label,
          Kind = unlock is Dish dish ? dish.Type.ToString() : unlock.UnlockGroup.ToString(),
          Difficulty = unlock is Dish d ? d.Difficulty : 0,
          IsHeld = heldIds.Contains( unlock.ID ),
          IsOurs = ours,
        } );
      }

      results.Sort(( a, b ) => {
        if ( a.IsOurs != b.IsOurs ) {
          return a.IsOurs ? -1 : 1;
        }

        return string.Compare( a.Name, b.Name, StringComparison.OrdinalIgnoreCase );
      } );

      return results;
    }

    // KitchenLib names custom GDOs "<guid> - <uniqueNameId>", so prefer the card's own
    // localised name.
    private static string DisplayName( Unlock unlock ) {
      var info = unlock.Info?.Get( Locale.English );
      if ( info != null && !string.IsNullOrEmpty( info.Name )) {
        return info.Name;
      }

      var raw = unlock.name ?? unlock.ID.ToString();
      var split = raw.LastIndexOf( " - ", StringComparison.Ordinal );
      return split >= 0 ? raw.Substring( split + 3 ) : raw;
    }

    internal HashSet<int> HeldIds() {
      var ids = new HashSet<int>();
      using ( var unlocks = held.ToComponentDataArray<CProgressionUnlock>( Allocator.Temp )) {
        foreach ( var unlock in unlocks ) {
          ids.Add( unlock.ID );
        }
      }

      return ids;
    }

    // Co-op progression is host-authoritative; granting from a client desyncs it.
    internal bool IsHost() => Session.HostIdentifier == 0;

    internal string GrantById( int id ) {
      foreach ( var card in BuildCards( null, false )) {
        if ( card.Id == id ) {
          return Grant( card );
        }
      }

      return "card not found";
    }

    internal string RemoveById( int id ) {
      foreach ( var card in BuildCards( null, false )) {
        if ( card.Id == id ) {
          return Remove( card );
        }
      }

      return "card not found";
    }

    internal string Grant( CardEntry card ) {
      if ( !IsHost()) {
        return "not the host, refusing to grant";
      }

      if ( HeldIds().Contains( card.Id )) {
        return $"'{card.Name}' is already held";
      }

      var entity = EntityManager.CreateEntity( typeof( CProgressionOption ));
      EntityManager.SetComponentData( entity, new CProgressionOption {
        ID = card.Id,
        FromFranchise = false,
      } );
      EntityManager.AddComponent<CProgressionOption.Selected>( entity );

      Debug.Log( $"[BeaverTails] picker granted '{card.Name}' (id {card.Id})" );
      return $"granted '{card.Name}'";
    }

    // Destroys the CMenuItem entities too, by SourceDish, or customers keep ordering a card the run no longer has.
    internal string Remove( CardEntry card ) {
      if ( !IsHost()) {
        return "not the host, refusing to remove";
      }

      var ourMenuItems = new HashSet<int>();
      if ( GameData.Main.TryGet<Dish>( card.Id, out var dish ) && dish.UnlocksMenuItems != null ) {
        foreach ( var menuItem in dish.UnlocksMenuItems ) {
          if ( menuItem.Item != null ) {
            ourMenuItems.Add( menuItem.Item.ID );
          }
        }
      }

      var unlockCount = DestroyWhere( held, ( CProgressionUnlock u ) => u.ID == card.Id );
      var menuCount = DestroyWhere( menuItems, ( CMenuItem m ) => m.SourceDish == card.Id );
      var ingredientCount = DestroyWhere(
          available, ( CAvailableIngredient a ) => ourMenuItems.Contains( a.MenuItem ));
      var extraCount = DestroyWhere(
          possibleExtras, ( CPossibleExtra e ) => ourMenuItems.Contains( e.MenuItem ));

      if ( unlockCount == 0 && menuCount == 0 ) {
        return $"'{card.Name}' is not held";
      }

      Debug.Log( $"[BeaverTails] picker removed '{card.Name}': {unlockCount} unlock, " +
                $"{menuCount} menu item, {ingredientCount} ingredient, {extraCount} extra" );

      return $"removed '{card.Name}' ({menuCount} menu items); orders already placed still stand";
    }

    private int DestroyWhere<T>( EntityQuery query, System.Func<T, bool> match )
        where T : struct, IComponentData {
      var destroyed = 0;
      using ( var entities = query.ToEntityArray( Allocator.Temp ))
      using ( var data = query.ToComponentDataArray<T>( Allocator.Temp )) {
        for ( var i = 0; i < data.Length; i++ ) {
          if ( !match( data[i] )) {
            continue;
          }

          EntityManager.DestroyEntity( entities[i] );
          destroyed++;
        }
      }

      return destroyed;
    }
  }

  internal struct CardEntry {
    public int Id;
    public string Name;
    public string Kind;
    public int Difficulty;
    public bool IsHeld;
    public bool IsOurs;
  }
}
