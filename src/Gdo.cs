// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2026 FireBall1725

using System.Collections.Generic;
using UnityEngine;
using KitchenData;
using KitchenLib.References;
using KitchenLib.Utils;

namespace BeaverTails {
  internal static class Gdo {
    public static Item Item( int id ) => GDOUtils.GetExistingGDO( id ) as Item;

    public static Appliance Appliance( int id ) => GDOUtils.GetExistingGDO( id ) as Appliance;

    // EditorCreateProvider fills the private field that CItemProvider.Attach would otherwise overwrite with 0.
    public static Kitchen.CItemProvider Provider(
        Item item,
        int maximum = 0,
        int available = 0,
        bool allowRefreshes = false,
        bool preventReturns = false ) {
      var provider = Kitchen.CItemProvider.EditorCreateProvider( item );
      provider.Maximum = maximum;
      provider.Available = available;
      provider.EmptyAtNight = false;
      provider.AllowRefreshes = allowRefreshes;
      provider.PreventReturns = preventReturns;
      return provider;
    }

    // Without DedicatedProvider, MinimumIngredients spawns the game's generic crate instead of ours.
    public static void PointItemAtProvider( string itemNameId, Appliance provider ) {
      var item = Own<Item>( itemNameId );
      if ( item == null || provider == null ) {
        UnityEngine.Debug.LogWarning( $"[BeaverTails] cannot give '{itemNameId}' a provider" );
        return;
      }

      item.DedicatedProvider = provider;
      UnityEngine.Debug.Log( $"[BeaverTails] {itemNameId} -> provider {provider.name}" );
    }

    // Odin scriptable objects need CreateInstance and HideAndDontSave, or Unity collects them mid-run.
    public static UnlockInfo Info( string name, string description, string flavour = "" ) {
      var info = UnityEngine.ScriptableObject.CreateInstance<UnlockInfo>();
      info.hideFlags = UnityEngine.HideFlags.HideAndDontSave;
      info.Locale = Locale.English;
      info.Name = name;
      info.Description = description;
      info.FlavourText = flavour;
      return info;
    }

    public static ApplianceInfo ApplianceInfo( string name, string description ) {
      var info = UnityEngine.ScriptableObject.CreateInstance<ApplianceInfo>();
      info.hideFlags = UnityEngine.HideFlags.HideAndDontSave;
      info.Locale = Locale.English;
      info.Name = name;
      info.Description = description;
      return info;
    }

    public static ProcessInfo ProcessInfo( string name, string icon ) {
      var info = UnityEngine.ScriptableObject.CreateInstance<ProcessInfo>();
      info.hideFlags = UnityEngine.HideFlags.HideAndDontSave;
      info.Locale = Locale.English;
      info.Name = name;
      info.Icon = icon;
      return info;
    }

    // Bounding box of every mesh under an object, in its own local space; visibleOnly skips what will not be drawn.
    public static bool LocalBounds(
        UnityEngine.GameObject target,
        out UnityEngine.Vector3 min,
        out UnityEngine.Vector3 max,
        bool visibleOnly = false ) {
      min = new UnityEngine.Vector3( float.MaxValue, float.MaxValue, float.MaxValue );
      max = new UnityEngine.Vector3( float.MinValue, float.MinValue, float.MinValue );

      if ( target == null ) {
        return false;
      }

      var found = false;
      var toLocal = target.transform.worldToLocalMatrix;

      foreach ( var filter in target.GetComponentsInChildren<UnityEngine.MeshFilter>( true )) {
        if ( filter.sharedMesh == null ) {
          continue;
        }

        if ( visibleOnly && !IsDrawn( filter.transform, target.transform )) {
          continue;
        }

        found = true;
        var matrix = toLocal * filter.transform.localToWorldMatrix;
        var bounds = filter.sharedMesh.bounds;

        for ( var corner = 0; corner < 8; corner++ ) {
          var point = matrix.MultiplyPoint3x4( new UnityEngine.Vector3(
              ( corner & 1 ) == 0 ? bounds.min.x : bounds.max.x,
              ( corner & 2 ) == 0 ? bounds.min.y : bounds.max.y,
              ( corner & 4 ) == 0 ? bounds.min.z : bounds.max.z ));

          min = UnityEngine.Vector3.Min( min, point );
          max = UnityEngine.Vector3.Max( max, point );
        }
      }

      return found;
    }

    // Activeness is checked up to root only, because every clone here is parked under an inactive holder.
    private static bool IsDrawn( UnityEngine.Transform mesh, UnityEngine.Transform root ) {
      var renderer = mesh.GetComponent<UnityEngine.Renderer>();
      if ( renderer == null || !renderer.enabled ) {
        return false;
      }

      for ( var node = mesh; node != null && node != root; node = node.parent ) {
        if ( !node.gameObject.activeSelf ) {
          return false;
        }
      }

      return true;
    }

    // Stand a model on an appliance without hiding it; across is its footprint as a fraction of the appliance's.
    public static UnityEngine.Transform PlaceOnAppliance(
        UnityEngine.GameObject appliance,
        string bundleAsset,
        UnityEngine.Color tint,
        float across = 0.34f,
        float sink = 0.03f,
        UnityEngine.Vector3 nudge = default ) {
      if ( appliance == null || !LocalBounds( appliance, out var min, out var max, true )) {
        UnityEngine.Debug.LogWarning( $"[BeaverTails] cannot place '{bundleAsset}'" );
        return null;
      }

      var model = CloneFromBundle( bundleAsset, bundleAsset, tint );
      if ( model == null || !LocalBounds( model, out var modelMin, out var modelMax, true )) {
        return null;
      }

      var target = max - min;
      var actual = modelMax - modelMin;

      var scale = 1f;
      if ( actual.x > 0f && actual.z > 0f ) {
        scale = UnityEngine.Mathf.Min(
            ( target.x * across ) / actual.x, ( target.z * across ) / actual.z );
      }

      model.transform.SetParent( appliance.transform, false );
      model.transform.localScale = UnityEngine.Vector3.one * scale;

      var drop = ( modelMax.y - modelMin.y ) * scale * sink;
      var placed = new UnityEngine.Vector3(
          (( min.x + max.x ) * 0.5f ) + nudge.x,
          max.y - drop + nudge.y,
          (( min.z + max.z ) * 0.5f ) + nudge.z );
      model.transform.localPosition = placed;

      return model.transform;
    }

    // Replace an appliance's visible geometry, scaled to the footprint the original occupied.
    public static UnityEngine.Transform ReskinAppliance(
        UnityEngine.GameObject appliance, string bundleAsset, UnityEngine.Color tint ) {
      if ( appliance == null || !LocalBounds( appliance, out var min, out var max )) {
        UnityEngine.Debug.LogWarning( $"[BeaverTails] cannot reskin with '{bundleAsset}'" );
        return null;
      }

      var model = CloneFromBundle( bundleAsset, bundleAsset, tint );
      return ReskinApplianceWith( appliance, model );
    }

    // fit shrinks the model to the donor's footprint, which is wrong for a model already a tile wide.
    public static UnityEngine.Transform ReskinApplianceWith(
        UnityEngine.GameObject appliance, UnityEngine.GameObject model, bool fit = true ) {
      if ( appliance == null || model == null || !LocalBounds( appliance, out var min, out var max )) {
        return null;
      }

      // Hide rather than destroy: some appliance renderers are driven by the game, and
      // deleting them has broken views before.
      foreach ( var renderer in appliance.GetComponentsInChildren<UnityEngine.Renderer>( true )) {
        renderer.enabled = false;
      }

      var skin = new UnityEngine.GameObject( "Skin" );
      skin.transform.SetParent( appliance.transform, false );

      model.transform.SetParent( skin.transform, false );
      model.transform.localPosition = UnityEngine.Vector3.zero;

      if ( !fit ) {
        skin.transform.localPosition = new UnityEngine.Vector3( 0f, min.y, 0f );
        return skin.transform;
      }

      if ( LocalBounds( model, out var modelMin, out var modelMax )) {
        var target = max - min;
        var actual = modelMax - modelMin;

        var scale = 1f;
        if ( actual.x > 0f && actual.z > 0f ) {
          scale = UnityEngine.Mathf.Min( target.x / actual.x, target.z / actual.z );
        }

        skin.transform.localScale = UnityEngine.Vector3.one * scale;
        skin.transform.localPosition = new UnityEngine.Vector3(
            ( min.x + max.x ) * 0.5f, min.y, ( min.z + max.z ) * 0.5f );
      }

      return skin.transform;
    }

    public static UnityEngine.GameObject AddToSkin(
        UnityEngine.Transform skin,
        string bundleAsset,
        UnityEngine.Color tint,
        UnityEngine.Vector3 position,
        float scale = 1f,
        float yaw = 0f ) {
      if ( skin == null ) {
        return null;
      }

      var model = CloneFromBundle( bundleAsset, bundleAsset, tint );
      if ( model == null ) {
        return null;
      }

      model.transform.SetParent( skin, false );
      model.transform.localPosition = position;
      model.transform.localScale = UnityEngine.Vector3.one * scale;
      model.transform.localRotation = UnityEngine.Quaternion.Euler( 0f, yaw, 0f );
      return model;
    }

    // Move a limited provider's item slots into our model; Items is private, hence the reflection.
    public static int PlaceProviderSlots(
        UnityEngine.GameObject appliance,
        UnityEngine.Transform parent,
        UnityEngine.Vector3[] slots,
        float scale ) {
      var view = appliance == null ? null : appliance.GetComponentInChildren<Kitchen.LimitedItemSourceView>( true );
      if ( view == null || parent == null ) {
        UnityEngine.Debug.LogWarning( "[BeaverTails] no LimitedItemSourceView to reposition" );
        return 0;
      }

      var field = typeof( Kitchen.LimitedItemSourceView ).GetField(
          "Items",
          System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance );

      var placeholders = field?.GetValue( view ) as System.Collections.Generic.List<UnityEngine.GameObject>;
      if ( placeholders == null ) {
        UnityEngine.Debug.LogWarning( "[BeaverTails] LimitedItemSourceView.Items not found" );
        return 0;
      }

      var placed = 0;
      for ( var i = 0; i < placeholders.Count; i++ ) {
        var placeholder = placeholders[i];
        if ( placeholder == null ) {
          continue;
        }

        if ( i >= slots.Length ) {
          // more slots than we have room for; stock never reaches them
          placeholder.SetActive( false );
          continue;
        }

        placeholder.transform.SetParent( parent, false );
        placeholder.transform.localPosition = slots[i];
        placeholder.transform.localRotation = UnityEngine.Quaternion.identity;
        placeholder.transform.localScale = UnityEngine.Vector3.one * scale;
        placed++;
      }

      return placed;
    }

    // A bar above the item, since PlateUp looks straight down and a lid hides anything at its own height.
    public static void AddLevelBar( UnityEngine.GameObject target, UnityEngine.Color colour ) {
      if ( target == null || !LocalBounds( target, out var min, out var max )) {
        return;
      }

      var size = max - min;
      var centreX = ( min.x + max.x ) * 0.5f;
      var centreZ = ( min.z + max.z ) * 0.5f;

      var y = max.y + ( size.y * 0.12f );
      var length = size.x * 0.86f;
      var depth = System.Math.Max( 0.02f, size.z * 0.16f );
      var thickness = System.Math.Max( 0.004f, size.y * 0.02f );

      var track = Bar( target, "LevelTrack", new UnityEngine.Color( 0.10f, 0.10f, 0.12f ));
      var bar = Bar( target, "LevelBar", UnityEngine.Color.Lerp( colour, UnityEngine.Color.white, 0.45f ));
      if ( track == null || bar == null ) {
        return;
      }

      track.transform.localScale = new UnityEngine.Vector3( length, thickness, depth );
      track.transform.localPosition = new UnityEngine.Vector3( centreX, y, centreZ );

      // The bar sits a hair proud of the track so it always wins the depth test.
      bar.transform.localScale = new UnityEngine.Vector3( length, thickness, depth * 0.62f );
      bar.transform.localPosition = new UnityEngine.Vector3(
          centreX, y + ( thickness * 0.6f ), centreZ );

      var view = target.GetComponent<SplitLevelView>() ?? target.AddComponent<SplitLevelView>();
      view.Bar = bar;
      view.BarFullLength = length;
      // Scaling a cube grows it about its centre, so the view moves it as it shortens or
      // the bar eats itself from both ends.
      view.BarLeftX = centreX - ( length * 0.5f );
      view.BarY = y + ( thickness * 0.6f );
      view.BarZ = centreZ;
    }

    // One flat box borrowing the target's own material, so it renders with PlateUp's shader
    // rather than a Unity default that is not in the build.
    private static UnityEngine.GameObject Bar(
        UnityEngine.GameObject target, string name, UnityEngine.Color colour ) {
      var filters = target.GetComponentsInChildren<UnityEngine.MeshFilter>( true );
      if ( filters.Length == 0 ) {
        return null;
      }

      var box = UnityEngine.GameObject.CreatePrimitive( UnityEngine.PrimitiveType.Cube );
      box.name = name;

      foreach ( var component in box.GetComponents<UnityEngine.Component>()) {
        if ( component is UnityEngine.Transform
            || component is UnityEngine.MeshFilter
            || component is UnityEngine.MeshRenderer ) {
          continue;
        }

        UnityEngine.Object.DestroyImmediate( component );
      }

      box.transform.SetParent( target.transform, false );

      var donor = filters[0].GetComponent<UnityEngine.Renderer>();
      var renderer = box.GetComponent<UnityEngine.Renderer>();
      if ( donor != null && donor.sharedMaterial != null && renderer != null ) {
        renderer.material = new UnityEngine.Material( donor.sharedMaterial );
      }

      Recolour( box, colour );
      return box;
    }

    // A pool of liquid inside a pot, sized from the pot's own mesh bounds.
    public static UnityEngine.GameObject AddContents(
        UnityEngine.GameObject pot,
        UnityEngine.Color colour,
        float fillHeight = 0.72f,
        float across = 0.74f ) {
      if ( pot == null ) {
        return null;
      }

      var filters = pot.GetComponentsInChildren<UnityEngine.MeshFilter>( true );
      if ( filters.Length == 0 ) {
        return null;
      }

      var toLocal = pot.transform.worldToLocalMatrix;
      var min = new UnityEngine.Vector3( float.MaxValue, float.MaxValue, float.MaxValue );
      var max = new UnityEngine.Vector3( float.MinValue, float.MinValue, float.MinValue );

      foreach ( var filter in filters ) {
        if ( filter.sharedMesh == null ) {
          continue;
        }

        var matrix = toLocal * filter.transform.localToWorldMatrix;
        var bounds = filter.sharedMesh.bounds;

        for ( var corner = 0; corner < 8; corner++ ) {
          var point = matrix.MultiplyPoint3x4( new UnityEngine.Vector3(
              ( corner & 1 ) == 0 ? bounds.min.x : bounds.max.x,
              ( corner & 2 ) == 0 ? bounds.min.y : bounds.max.y,
              ( corner & 4 ) == 0 ? bounds.min.z : bounds.max.z ));

          min = UnityEngine.Vector3.Min( min, point );
          max = UnityEngine.Vector3.Max( max, point );
        }
      }

      var size = max - min;
      if ( size.x <= 0f || size.z <= 0f ) {
        return null;
      }

      var disc = UnityEngine.GameObject.CreatePrimitive( UnityEngine.PrimitiveType.Cylinder );
      disc.name = "Contents";

      // Collider lives in a physics assembly this mod does not reference, so strip by exclusion.
      foreach ( var component in disc.GetComponents<UnityEngine.Component>()) {
        if ( component is UnityEngine.Transform
            || component is UnityEngine.MeshFilter
            || component is UnityEngine.MeshRenderer ) {
          continue;
        }

        UnityEngine.Object.DestroyImmediate( component );
      }

      disc.transform.SetParent( pot.transform, false );

      // A primitive cylinder is 1 across and 2 tall, so x/z are diameters and y is a half-
      // height.
      disc.transform.localScale = new UnityEngine.Vector3(
          size.x * across, System.Math.Max( 0.004f, size.y * 0.02f ), size.z * across );
      disc.transform.localPosition = new UnityEngine.Vector3(
          ( min.x + max.x ) * 0.5f, min.y + ( size.y * fillHeight ), ( min.z + max.z ) * 0.5f );

      var donor = filters[0].GetComponent<UnityEngine.Renderer>();
      var renderer = disc.GetComponent<UnityEngine.Renderer>();
      if ( donor != null && donor.sharedMaterial != null && renderer != null ) {
        renderer.material = new UnityEngine.Material( donor.sharedMaterial );
      }

      Recolour( disc, colour );
      return disc;
    }

    // Geometry only: cloning a working appliance duplicates its view identifiers and blacks out the HQ.
    public static UnityEngine.GameObject ApplianceVisual( int sourceId, string name ) =>
        ApplianceVisual( Appliance( sourceId ), name );

    // Same, from an Appliance we already have, which is the only way to reach another mod's
    // appliance because GetExistingGDO cannot see it.
    public static UnityEngine.GameObject ApplianceVisual( Appliance source, string name ) {
      var prefab = source?.Prefab;
      if ( prefab == null ) {
        UnityEngine.Debug.LogWarning( $"[BeaverTails] no prefab to take a visual from for '{name}'" );
        return null;
      }

      var clone = UnityEngine.Object.Instantiate( prefab, Holder());
      clone.name = name;

      foreach ( var component in clone.GetComponentsInChildren<UnityEngine.Component>( true )) {
        if ( component == null
            || component is UnityEngine.Transform
            || component is UnityEngine.MeshFilter
            || component is UnityEngine.MeshRenderer ) {
          continue;
        }

        UnityEngine.Object.DestroyImmediate( component );
      }

      // Do not switch renderers on; the game keeps some off and they inflate the bounds.
      return clone;
    }

    // Passing no tint is not the same as passing white, which flattens the panelling worth borrowing.
    public static UnityEngine.GameObject CloneAppliancePrefab(
        int sourceId, string name, UnityEngine.Color? tint = null ) {
      var source = Appliance( sourceId )?.Prefab;
      if ( source == null ) {
        UnityEngine.Debug.LogWarning( $"[BeaverTails] no prefab on source appliance {sourceId} for '{name}'" );
        return null;
      }

      var clone = UnityEngine.Object.Instantiate( source, Holder());
      clone.name = name;
      return tint.HasValue ? Recolour( clone, tint.Value ) : clone;
    }

    // The model shown while an appliance is carried, a separate prefab from Prefab; leaving
    // it unset is why moving a jar rack showed a rack of pots.
    public static UnityEngine.GameObject CloneHeldAppliancePrefab(
        int sourceId, string name, UnityEngine.Color tint ) {
      var appliance = Appliance( sourceId );
      var source = appliance?.HeldAppliancePrefab ?? appliance?.Prefab;
      if ( source == null ) {
        return null;
      }

      var clone = UnityEngine.Object.Instantiate( source, Holder());
      clone.name = name + " (Held)";
      return Recolour( clone, tint );
    }

    // A custom item defaults to ItemStorage.None, which every holder rejects.
    public static void AllowStorage( Item item ) {
      if ( item == null ) {
        return;
      }

      item.ItemStorageFlags = ItemStorage.StackableFood | ItemStorage.Small;
    }

    // Keep an appliance out of the shop until a held card needs its ingredient, because
    // ShopBuilder's IsRequired returns true for any appliance with empty requirement lists.
    public static void RequireForShop( Appliance appliance, Item ingredient ) {
      if ( appliance == null || ingredient == null ) {
        return;
      }

      if ( appliance.RequiresIngredientForShop == null ) {
        appliance.RequiresIngredientForShop = new System.Collections.Generic.List<Item>();
      }

      if ( !appliance.RequiresIngredientForShop.Contains( ingredient )) {
        appliance.RequiresIngredientForShop.Add( ingredient );
      }
    }

    public static Process Process( int id ) => GDOUtils.GetExistingGDO( id ) as Process;

    // The Fryer Appliance mod's deep-fry process, specific to a fryer unlike the generic
    // Cook.
    public static Process FryProcess() =>
        GDOUtils.GetCustomGameDataObject<KitchenFryer.FryMe>()?.GameDataObject as Process;

    // Teaches weak variants too, and must run from OnRegister since the process index is built once.
    public static void TeachProcess( Process process, int[] applianceIds, string label ) {
      if ( process == null ) {
        Debug.LogError( $"[BeaverTails] {label}: process is null, taught nothing" );
        return;
      }

      var seen = new HashSet<int>();
      var taught = new List<string>();

      foreach ( var id in applianceIds ) {
        for ( var appliance = Appliance( id ); appliance != null; appliance = appliance.WeakVariant ) {
          if ( !seen.Add( appliance.ID )) {
            break;
          }

          if ( appliance.Processes == null ) {
            appliance.Processes = new List<Appliance.ApplianceProcesses>();
          }

          appliance.Processes.Add( new Appliance.ApplianceProcesses {
            Process = process,
            IsAutomatic = true,
            Speed = 1f,
            Validity = ProcessValidity.Generic,
          } );

          taught.Add( appliance.name );
        }
      }

      Debug.Log( $"[BeaverTails] {label} taught to {taught.Count}: "
                + string.Join( ", ", taught.ToArray()));
    }

    public static T Own<T>( string uniqueNameId ) where T : GameDataObject =>
        GDOUtils.GetCustomGameDataObject( BeaverTailsMod.Guid, uniqueNameId )?.GameDataObject as T;

    // The keys IngredientLib indexes by, its own classes' NameTag lowercased, as constants
    // because a typo logs and returns null rather than failing the build.
    public static class LibKeys {
      public const string Cinnamon = "cinnamon";
      public const string Caramel = "caramel";
      public const string Banana = "banana";
      public const string PeeledBanana = "peeled banana";
      public const string ChoppedBanana = "chopped banana";
      public const string Honey = "honey";
      public const string Crackers = "crackers";
      public const string Vinegar = "vinegar";
      public const string Syrup = "syrup";
    }

    // Call lazily, never from a field initialiser: IngredientLib fills its dictionaries during its own Convert.
    public static Item Lib( string name ) => LibLookup<Item>(
        name, IngredientLib.References.ingredientReferences, "ingredient" );

    // The portion poured out of a splittable container, for the ones that have one.
    public static Item LibSplit( string name ) => LibLookup<Item>(
        name, IngredientLib.References.splitIngredientReferences, "split ingredient" );

    public static Appliance LibProvider( string name ) => LibLookup<Appliance>(
        name, IngredientLib.References.providerReferences, "provider" );

    private static T LibLookup<T>(
        string name,
        System.Collections.Generic.Dictionary<string, int> index,
        string kind )
        where T : GameDataObject {
      if ( name == null || index == null || !index.TryGetValue( name.ToLowerInvariant(), out var id )) {
        UnityEngine.Debug.LogWarning(
            $"[BeaverTails] IngredientLib has no {kind} '{name}'. Either it is not "
            + "installed, or this was read before its Convert ran." );
        return null;
      }

      // Custom registry first, base game second, since another mod's GDOs are not in the base-game index.
      var found = GDOUtils.GetCustomGameDataObject( id )?.GameDataObject as T
                  ?? GDOUtils.GetExistingGDO( id ) as T;

      if ( found == null ) {
        UnityEngine.Debug.LogWarning(
            $"[BeaverTails] IngredientLib {kind} '{name}' is id {id}, which resolved to "
            + $"nothing that is a {typeof( T ).Name}. Recipes using it will be broken." );
      }

      return found;
    }

    // Recolour using PlateUp's Simple Flat shader taken from a base-game material, since
    // Unity has no idea what that shader is and the bundle ships geometry only.
    public static UnityEngine.GameObject Recolour( UnityEngine.GameObject target, UnityEngine.Color colour ) {
      if ( target == null ) {
        return null;
      }

      var donor = Item( ItemReferences.Dough )?.Prefab
          ?.GetComponentInChildren<UnityEngine.Renderer>( true )?.sharedMaterial;

      foreach ( var renderer in target.GetComponentsInChildren<UnityEngine.Renderer>( true )) {
        var source = donor != null ? donor : renderer.sharedMaterial;
        if ( source == null ) {
          continue;
        }

        var copy = new UnityEngine.Material( source );
        if ( copy.HasProperty( "_Color0" )) {
          copy.SetColor( "_Color0", colour );
        }

        if ( copy.HasProperty( "_BaseColor" )) {
          copy.SetColor( "_BaseColor", colour );
        }

        renderer.sharedMaterial = copy;
      }

      return target;
    }

    // A private copy, because LoadAsset returns the same shared object every call.
    public static UnityEngine.GameObject CloneFromBundle( string assetName, string cloneName, UnityEngine.Color tint ) {
      var source = BeaverTailsMod.Bundle?.LoadAsset<UnityEngine.GameObject>( assetName );
      if ( source == null ) {
        UnityEngine.Debug.LogWarning( $"[BeaverTails] bundle has no asset '{assetName}'" );
        return null;
      }

      var clone = UnityEngine.Object.Instantiate( source, Holder());
      clone.name = cloneName;
      return Recolour( clone, tint );
    }

    // Parent for prefab clones; it is inactive, but the clones stay active because
    // Object.Instantiate copies activeSelf and an inactive clone spawns invisible items.
    private static UnityEngine.GameObject holder;

    // The serving mesh is authored seated on the dish floor, so it parents at Vector3.zero.
    public static UnityEngine.GameObject Portion(
        string name, string dishAsset, string servingAsset,
        UnityEngine.Color serving, string colourBlindTag = null ) {
      var dish = CloneFromBundle( dishAsset, name, DishNeutral );
      if ( dish == null ) {
        UnityEngine.Debug.LogWarning( $"[BeaverTails] no '{dishAsset}' to build '{name}'" );
        return null;
      }

      var contents = CloneFromBundle( servingAsset, name + " Contents", serving );
      if ( contents == null ) {
        UnityEngine.Debug.LogWarning( $"[BeaverTails] no '{servingAsset}' for '{name}'" );
        return dish;
      }

      contents.transform.SetParent( dish.transform, false );
      contents.transform.localPosition = UnityEngine.Vector3.zero;

      Relabel( dish, colourBlindTag );
      return dish;
    }

    public static UnityEngine.Color DishNeutral => new UnityEngine.Color( 0.86f, 0.84f, 0.80f );

    // A cup with a serving in it; fillAsset is one of CupLiquid, CupChunks or CupDome.
    public static UnityEngine.GameObject MeasuringCup(
        string name, UnityEngine.Color fill, string colourBlindTag = null,
        string fillAsset = "CupLiquid" ) =>
        Portion( name, "PortionCup", fillAsset, fill, colourBlindTag );

    // KitchenLib stamps ColourBlindTag during Convert, so a replaced prefab inherits the donor's tag.
    private static void Relabel( UnityEngine.GameObject prefab, string tag ) {
      if ( prefab == null || string.IsNullOrEmpty( tag )) {
        return;
      }

      var title = FindChild( prefab.transform, "Title" );
      var text = title == null ? null : title.GetComponent<TMPro.TMP_Text>();
      if ( text == null ) {
        UnityEngine.Debug.LogWarning(
            $"[BeaverTails] '{prefab.name}' has no colourblind Title to relabel '{tag}'" );
        return;
      }

      text.text = tag;
    }

    // Depth-first by name, case insensitive, because a bundle's child names are not ours to
    // rely on.
    public static UnityEngine.Transform FindChild( UnityEngine.Transform root, string name ) {
      if ( root == null ) {
        return null;
      }

      foreach ( var child in root.GetComponentsInChildren<UnityEngine.Transform>( true )) {
        if ( string.Equals( child.name, name, System.StringComparison.OrdinalIgnoreCase )) {
          return child;
        }
      }

      return null;
    }

    private static UnityEngine.Transform Holder() {
      if ( holder == null ) {
        holder = new UnityEngine.GameObject( "BeaverTailsPrefabHolder" );
        holder.SetActive( false );
        UnityEngine.Object.DontDestroyOnLoad( holder );
      }

      return holder.transform;
    }

    public static UnityEngine.Transform Parked() => Holder();

    // An empty object to assemble a prefab under, parked out of the scene, because a bare new
    // GameObject lands in the active scene and renders immediately.
    public static UnityEngine.GameObject NewParked( string name ) {
      var parked = new UnityEngine.GameObject( name );
      parked.transform.SetParent( Holder(), false );
      return parked;
    }

    // Only clone sources that render unconditionally; the doughnut prefabs toggle meshes through their own view.
    public static UnityEngine.GameObject ClonePrefab(
        int sourceItemId,
        string name,
        UnityEngine.Vector3? scale = null,
        UnityEngine.Color? tint = null ) {
      var source = Item( sourceItemId )?.Prefab;
      if ( source == null ) {
        UnityEngine.Debug.LogWarning( $"[BeaverTails] no prefab on source item {sourceItemId} for '{name}'" );
        return null;
      }

      var clone = UnityEngine.Object.Instantiate( source, Holder());
      clone.name = name;

      if ( scale.HasValue ) {
        // The mesh sits on a child, and scaling the root is pointless because the view
        // rewrites the root transform when it spawns the item.
        for ( var i = 0; i < clone.transform.childCount; i++ ) {
          clone.transform.GetChild( i ).localScale = scale.Value;
        }
      }

      if ( tint.HasValue ) {
        foreach ( var renderer in clone.GetComponentsInChildren<UnityEngine.Renderer>( true )) {
          var materials = renderer.sharedMaterials;
          for ( var i = 0; i < materials.Length; i++ ) {
            if ( materials[i] == null ) {
              continue;
            }

            var copy = new UnityEngine.Material( materials[i] );

            // Simple Flat keeps its colour in _Color0; there is no _Color, which is
            // why setting material.color was a silent no-op.
            if ( copy.HasProperty( "_Color0" )) {
              copy.SetColor( "_Color0", tint.Value );
            }

            if ( copy.HasProperty( "_BaseColor" )) {
              copy.SetColor( "_BaseColor", tint.Value );
            }

            materials[i] = copy;
          }

          renderer.sharedMaterials = materials;
        }
      }

      return clone;
    }
  }
}
