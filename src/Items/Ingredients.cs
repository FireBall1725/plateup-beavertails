// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2026 FireBall1725

using System.Collections.Generic;
using KitchenData;
using KitchenLib.Customs;
using KitchenLib.References;
using UnityEngine;

namespace BeaverTails {
  // A counter bin of Reese's Pieces, three meshes because Simple Flat has one _Color0 per material so mixed candy cannot come from one mesh.
  public class ReesesPiecesItem : CustomItem {
    public const string NameId = "reeses_pieces";

    public static Color CandyOrange => new Color( 0.93f, 0.44f, 0.09f );

    public override string UniqueNameID => NameId;

    private GameObject prefab;

    public override GameObject Prefab {
      get => prefab ?? ( prefab = BuildPrefab());
      protected set { }
    }

    // Shared with the bin, which shows the same heap sitting inside it.
    internal static GameObject BuildPile( string name ) {
      var root = Gdo.NewParked( name );

      var candy = new (string Asset, Color Colour)[]
      {
                ("CandyPileOrange", CandyOrange),
                ("CandyPileYellow", new Color(0.96f, 0.76f, 0.13f)),
                ("CandyPileBrown", new Color(0.40f, 0.22f, 0.11f)),
      };

      foreach ( var (asset, colour) in candy ) {
        var pieces = Gdo.CloneFromBundle( asset, asset, colour );
        if ( pieces == null ) {
          continue;
        }

        pieces.transform.SetParent( root.transform, false );
        pieces.transform.localPosition = Vector3.zero;
      }

      return root;
    }

    private static GameObject BuildPrefab() => BuildPile( "Reese's Pieces" );

    public override ItemValue ItemValue {
      get => ItemValue.Small;
      protected set { }
    }

    public override ItemCategory ItemCategory {
      get => ItemCategory.Generic;
      protected set { }
    }

    public override string ColourBlindTag {
      get => "RP";
      protected set { }
    }
  }

  // Crumbs, shards and chunks that finish a tail: a new one is a name, a colour, a tag and which bundle mesh it uses.
  public abstract class ScatterToppingItem : CustomItem {
    protected abstract string DisplayName { get; }

    protected abstract Color Colour { get; }

    // angular shards by default, because most of these are crushed rather than moulded
    protected virtual string BundleAsset => "SkorPile";

    private GameObject prefab;

    public override GameObject Prefab {
      get => prefab ?? ( prefab = Gdo.CloneFromBundle( BundleAsset, DisplayName, Colour ));
      protected set { }
    }

    public override ItemValue ItemValue {
      get => ItemValue.Small;
      protected set { }
    }

    public override ItemCategory ItemCategory {
      get => ItemCategory.Generic;
      protected set { }
    }
  }

  public class PistachioCrumbsItem : ScatterToppingItem {
    public const string NameId = "pistachio_crumbs";

    public static Color Green => new Color( 0.55f, 0.66f, 0.35f );

    public override string UniqueNameID => NameId;

    protected override string DisplayName => "Pistachio Crumbs";

    protected override Color Colour => Green;

    public override string ColourBlindTag {
      get => "Pr";
      protected set { }
    }
  }

  public class CrushedOreoItem : ScatterToppingItem {
    public const string NameId = "crushed_oreo";

    public static Color Cookie => new Color( 0.16f, 0.15f, 0.16f );

    public override string UniqueNameID => NameId;

    protected override string DisplayName => "Crushed Cookies";

    protected override Color Colour => Cookie;

    public override string ColourBlindTag {
      get => "Or";
      protected set { }
    }
  }

  // Chunks rather than crumbs, with their own heap so the bin is not eleven pieces deep.
  public class WhiteChocolateChunksItem : ScatterToppingItem {
    public const string NameId = "white_chocolate_chunks";

    public static Color Cream => new Color( 0.96f, 0.92f, 0.80f );

    public override string UniqueNameID => NameId;

    protected override string DisplayName => "White Chocolate Chunks";

    protected override Color Colour => Cream;

    protected override string BundleAsset => "CandyPileWhite";

    public override string ColourBlindTag {
      get => "Wc";
      protected set { }
    }
  }

  // Same infinite counter bin as the Reese's Pieces, one colour and its own angular shard mesh.
  public class SkorBitsItem : CustomItem {
    public const string NameId = "skor_bits";

    public static Color Toffee => new Color( 0.55f, 0.31f, 0.11f );

    public override string UniqueNameID => NameId;

    private GameObject prefab;

    public override GameObject Prefab {
      get => prefab ?? ( prefab = Gdo.CloneFromBundle( "SkorPile", "Skor Bits", Toffee ));
      protected set { }
    }

    public override ItemValue ItemValue {
      get => ItemValue.Small;
      protected set { }
    }

    public override ItemCategory ItemCategory {
      get => ItemCategory.Generic;
      protected set { }
    }

    public override string ColourBlindTag {
      get => "Sk";
      protected set { }
    }
  }

  // Premixed cinnamon sugar, free because merging concatenates component lists, so one application satisfies both of the Classic's sets.
  public class CinnamonSugarItem : CustomItemGroup {
    public const string NameId = "cinnamon_sugar";

    public override string UniqueNameID => NameId;

    private GameObject prefab;

    public override GameObject Prefab {
      get => prefab ?? ( prefab = Gdo.ClonePrefab(
          ItemReferences.Sugar, "Cinnamon Sugar", tint: new Color( 0.78f, 0.62f, 0.42f )));
      protected set { }
    }

    public override List<ItemGroup.ItemSet> Sets {
      get => new List<ItemGroup.ItemSet>
      {
                new ItemGroup.ItemSet
                {
                    Min = 1,
                    Max = 1,
                    IsMandatory = false,
                    Items = new List<Item> { Gdo.Lib(Gdo.LibKeys.Cinnamon) },
                },
                new ItemGroup.ItemSet
                {
                    Min = 1,
                    Max = 1,
                    IsMandatory = false,
                    Items = new List<Item> { Gdo.Item(ItemReferences.Sugar) },
                },
            };
      protected set { }
    }

    public override ItemValue ItemValue {
      get => ItemValue.Small;
      protected set { }
    }

    public override string ColourBlindTag {
      get => "Cx";
      protected set { }
    }

    public override void OnRegister( ItemGroup gameDataObject ) {
      base.OnRegister( gameDataObject );
      Gdo.AllowStorage( gameDataObject );
    }
  }
}
