// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2026 FireBall1725

using System.Collections.Generic;
using Kitchen;
using KitchenData;
using KitchenLib.Customs;
using KitchenLib.References;
using UnityEngine;

namespace BeaverTails {
  // Maximum = 0 is what infinite means to CItemProvider.
  public abstract class ToppingBinAppliance : CustomAppliance {
    protected abstract string ItemNameId { get; }

    protected abstract string DisplayName { get; }

    protected abstract string BinDescription { get; }

    // The glass, not the contents, left near-white so the sweets read.
    protected virtual Color JarColour => new Color( 0.92f, 0.93f, 0.96f );

    private GameObject prefab;

    public override GameObject Prefab {
      get => prefab ?? ( prefab = BuildPrefab( $"Source - {DisplayName}" ));
      protected set { }
    }

    private GameObject heldPrefab;

    public override GameObject HeldAppliancePrefab {
      get => heldPrefab ?? ( heldPrefab = BuildPrefab( $"{DisplayName} (Held)" ));
      protected set { }
    }

    // PlateUp's own countertop, borrowed because a geometry-only bundle cannot match its outline.
    private const float JarFootprint = 0.34f;
    private const float ContentsHeightInJar = 0.55f;
    // fallback only, used if either set of bounds cannot be measured
    private const float ContentsScale = 0.55f;

    // fraction of the jar's width the heap should fill
    private const float ContentsFillsJar = 0.72f;
    // How far into the worktop the jar settles, as a fraction of its own scaled height.
    private const float JarSink = 0.03f;

    // No nudge, since a fixed offset only holds at one rotation.
    private static Vector3 JarNudge => Vector3.zero;

    private GameObject BuildPrefab( string name ) {
      // IngredientLib's syrup source, whose bottle transform is a known-good anchor for our jar.
      var bin = Gdo.CloneAppliancePrefab( ApplianceReferences.SourceNuts, name );
      if ( bin == null ) {
        return null;
      }

      // It wears a countertop, so it has to BE a countertop to walk past. The nut dispenser
      // underneath is a different shape and off-centre, so a cook moving along a row of
      // counters catches on ours, at some rotations more than others.
      Gdo.MatchCollision( bin, ApplianceReferences.Countertop );

      // The counter is the base game's; IngredientLib applies its materials too late and theirs comes out magenta.
      var skin = Gdo.ReskinApplianceWith(
          bin, Gdo.ApplianceVisual( ApplianceReferences.Countertop, "Counter" ), fit: false );
      if ( skin == null ) {
        return bin;
      }

      // Parent to the counter mesh, not the skin: an appliance rotates about its origin.
      var counter = skin.childCount > 0 ? skin.GetChild( 0 ).gameObject : skin.gameObject;
      var jar = Gdo.PlaceOnAppliance(
          counter, "CandyJar", JarColour, JarFootprint, JarSink, JarNudge );
      if ( jar == null ) {
        return bin;
      }

      FillJar( jar );
      return bin;
    }

    private void FillJar( Transform jar ) {
      var contents = BuildContents();
      if ( contents == null || !Gdo.LocalBounds( jar.gameObject, out var jarMin, out var jarMax )) {
        return;
      }

      contents.name = "Bin Contents";
      contents.transform.SetParent( jar, false );

      // The heap inherits every scaling applied to the jar, on top of the bundle's own 0.30.
      var jarWidth = jarMax.x - jarMin.x;
      var scale = ContentsScale;
      if ( Gdo.LocalBounds( contents, out var heapMin, out var heapMax )) {
        var heapWidth = heapMax.x - heapMin.x;
        if ( heapWidth > 0f && jarWidth > 0f ) {
          scale = ( jarWidth * ContentsFillsJar ) / heapWidth;
        }
      }

      contents.transform.localScale = Vector3.one * scale;
      contents.transform.localPosition = new Vector3(
          0f, ( jarMax.y - jarMin.y ) * ContentsHeightInJar, 0f );
    }

    // Reese's Pieces overrides this because its heap is three meshes in three colours.
    protected virtual GameObject BuildContents() {
      var item = Gdo.Own<Item>( ItemNameId );
      return item?.Prefab == null ? null : Object.Instantiate( item.Prefab, Gdo.Parked());
    }

    public override List<IApplianceProperty> Properties {
      get {
        var topping = Gdo.Own<Item>( ItemNameId );
        if ( topping == null ) {
          return new List<IApplianceProperty>();
        }

        return new List<IApplianceProperty> { Gdo.Provider( topping ) };
      }
      protected set { }
    }

    public override bool IsPurchasable {
      get => false;
      protected set { }
    }

    private List<(Locale, ApplianceInfo)> cachedInfo;

    public override List<(Locale, ApplianceInfo)> InfoList {
      get => cachedInfo ?? ( cachedInfo = new List<(Locale, ApplianceInfo)>
      {
                (Locale.English, Gdo.ApplianceInfo(DisplayName, BinDescription)),
            } );
      protected set { }
    }

    // Hands the item its provider back, which is what makes a dispenser appear in MinimumIngredients.
    public override void OnRegister( Appliance gameDataObject ) {
      base.OnRegister( gameDataObject );
      Gdo.PointItemAtProvider( ItemNameId, gameDataObject );
      Gdo.RequireForShop( gameDataObject, Gdo.Own<Item>( ItemNameId ));
    }
  }

  public class ReesesBinAppliance : ToppingBinAppliance {
    public const string NameId = "reeses_bin";

    public override string UniqueNameID => NameId;

    protected override string ItemNameId => ReesesPiecesItem.NameId;

    protected override string DisplayName => "Reese's Pieces";

    protected override string BinDescription => "A bin of peanut butter candy. Never runs out.";

    // No BuildContents override, so the pile keeps the Colour Blind child KitchenLib stamps on.
  }

  public class SkorBinAppliance : ToppingBinAppliance {
    public const string NameId = "skor_bin";

    public override string UniqueNameID => NameId;

    protected override string ItemNameId => SkorBitsItem.NameId;

    protected override string DisplayName => "Skor Bits";

    protected override string BinDescription => "A bin of toffee bits. Never runs out.";

    protected override Color JarColour => new Color( 0.82f, 0.80f, 0.78f );
  }

  public class PistachioBinAppliance : ToppingBinAppliance {
    public const string NameId = "pistachio_bin";

    public override string UniqueNameID => NameId;

    protected override string ItemNameId => PistachioCrumbsItem.NameId;

    protected override string DisplayName => "Pistachio Crumbs";

    protected override string BinDescription => "A bin of chopped pistachios. Never runs out.";

    // Warm, so the pistachio bin is not the same near-white as the Reese's bin beside it.
    protected override Color JarColour => new Color( 0.86f, 0.81f, 0.70f );
  }

  public class OreoBinAppliance : ToppingBinAppliance {
    public const string NameId = "oreo_bin";

    public override string UniqueNameID => NameId;

    protected override string ItemNameId => CrushedOreoItem.NameId;

    protected override string DisplayName => "Crushed Cookies";

    protected override string BinDescription =>
        "A bin of crushed chocolate sandwich cookies. Never runs out.";

    protected override Color JarColour => new Color( 0.78f, 0.78f, 0.80f );
  }

  public class WhiteChocolateBinAppliance : ToppingBinAppliance {
    public const string NameId = "white_chocolate_bin";

    public override string UniqueNameID => NameId;

    protected override string ItemNameId => WhiteChocolateChunksItem.NameId;

    protected override string DisplayName => "White Chocolate Chunks";

    protected override string BinDescription =>
        "A bin of white chocolate chunks. Never runs out.";

    // Cream chunks needed a smoky glass; in a near-white jar they were only dE 20 from it.
    protected override Color JarColour => new Color( 0.58f, 0.60f, 0.65f );
  }
}
