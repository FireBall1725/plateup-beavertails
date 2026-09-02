// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2026 FireBall1725

using System.Collections.Generic;
using Kitchen;
using KitchenData;
using KitchenLib.Customs;
using KitchenLib.References;
using UnityEngine;

namespace BeaverTails {
  // The two pot chains poutine needs: roux and meat into gravy, milk and vinegar into curds.

  // A portion in a measuring cup, built in two stages so Prefab is never null through Convert.
  public abstract class CupPortionItem : CustomItem {
    protected abstract string DisplayName { get; }

    protected abstract Color Contents { get; }

    private GameObject prefab;

    // OnRegister is where the colourblind tag is rewritten, so the two-stage build stays.
    public override GameObject Prefab {
      get => prefab ?? ( prefab = Gdo.MeasuringCup( DisplayName, Contents, ColourBlindTag ));
      protected set { }
    }

    public override void OnRegister( Item gameDataObject ) {
      base.OnRegister( gameDataObject );

      var cup = Gdo.MeasuringCup( DisplayName, Contents, ColourBlindTag );
      if ( cup != null && gameDataObject != null ) {
        prefab = cup;
        gameDataObject.Prefab = cup;
      }
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

  // A cooked pot that gives four portions and hands the pot back.
  public abstract class SplittablePotItem : CustomItem {
    protected abstract string DisplayName { get; }

    protected abstract Color Contents { get; }

    protected abstract string PortionNameId { get; }

    private GameObject prefab;

    public override GameObject Prefab {
      get => prefab ?? ( prefab = PotWithSugarItem.BuildSplittablePrefab( DisplayName, Contents ));
      protected set { }
    }

    public override Item SplitSubItem {
      get => Gdo.Own<Item>( PortionNameId );
      protected set { }
    }

    public override int SplitCount {
      get => 4;
      protected set { }
    }

    // the pot is worth keeping, unlike a spread jar
    public override List<Item> SplitDepletedItems {
      get => new List<Item> { Gdo.Item( ItemReferences.Pot ) };
      protected set { }
    }

    public override Item DisposesTo {
      get => Gdo.Item( ItemReferences.Pot );
      protected set { }
    }

    // Burns if left on the hob, like every other pot in the mod.
    public override List<Item.ItemProcess> Processes {
      get => new List<Item.ItemProcess>
      {
                new Item.ItemProcess
                {
                    Process = Gdo.Own<Process>(CaramelizeProcess.NameId),
                    Result = Gdo.Own<Item>(PotWithBurnedItem.NameId),
                    Duration = 30,
                    IsBad = true,
                },
            };
      protected set { }
    }

    public override ItemValue ItemValue {
      get => ItemValue.Medium;
      protected set { }
    }

    public override ItemCategory ItemCategory {
      get => ItemCategory.Generic;
      protected set { }
    }
  }

  public class GravyItem : CupPortionItem {
    public const string NameId = "gravy";

    public static Color Rich => new Color( 0.30f, 0.19f, 0.10f );

    public override string UniqueNameID => NameId;

    protected override string DisplayName => "Gravy";

    protected override Color Contents => Rich;

    public override string ColourBlindTag {
      get => "Gv";
      protected set { }
    }
  }

  public class PotWithGravyItem : SplittablePotItem {
    public const string NameId = "pot_with_gravy";

    public override string UniqueNameID => NameId;

    protected override string DisplayName => "Pot With Gravy";

    protected override Color Contents => GravyItem.Rich;

    protected override string PortionNameId => GravyItem.NameId;

    public override string ColourBlindTag {
      get => "Pg";
      protected set { }
    }
  }

  // Chopped meat into the base game's cooked roux, then cooked down to gravy.
  public class PotWithGravyRawItem : CustomItemGroup {
    public const string NameId = "pot_with_gravy_raw";

    public override string UniqueNameID => NameId;

    private GameObject prefab;

    public override GameObject Prefab {
      get => prefab ?? ( prefab = PotWithSugarItem.BuildPrefab(
          "Pot With Gravy (Raw)", new Color( 0.62f, 0.44f, 0.26f )));
      protected set { }
    }

    // No pot set: the pot came along inside the roux.
    public override List<ItemGroup.ItemSet> Sets {
      get => new List<ItemGroup.ItemSet>
      {
                new ItemGroup.ItemSet
                {
                    Min = 2,
                    Max = 2,
                    IsMandatory = false,
                    Items = new List<Item>
                    {
                        Gdo.Item(ItemReferences.RouxCooked),
                        Gdo.Item(ItemReferences.MeatChopped),
                    },
                },
            };
      protected set { }
    }

    public override List<Item.ItemProcess> Processes {
      get => new List<Item.ItemProcess>
      {
                new Item.ItemProcess
                {
                    Process = Gdo.Own<Process>(CaramelizeProcess.NameId),
                    Result = Gdo.Own<Item>(PotWithGravyItem.NameId),
                    Duration = 15,
                },
            };
      protected set { }
    }

    public override Item DisposesTo {
      get => Gdo.Item( ItemReferences.Pot );
      protected set { }
    }

    public override ItemValue ItemValue {
      get => ItemValue.Medium;
      protected set { }
    }

    public override string ColourBlindTag {
      get => "PV";
      protected set { }
    }
  }

  // Curds are lumps, not a liquid, so they get the rounded chunk mesh rather than a cup.
  public class CheeseCurdsItem : ScatterToppingItem {
    public const string NameId = "cheese_curds";

    public static Color Curd => new Color( 0.98f, 0.92f, 0.68f );

    public override string UniqueNameID => NameId;

    protected override string DisplayName => "Cheese Curds";

    protected override Color Colour => Curd;

    // the poutine's own curd mesh, so a portion in your hand matches what lands in the tray
    protected override string BundleAsset => "PoutineCurds";

    // Into the fryer for the double cheese version.
    public override List<Item.ItemProcess> Processes {
      get => new List<Item.ItemProcess>
      {
                new Item.ItemProcess
                {
                    Process = Gdo.FryProcess(),
                    Result = Gdo.Own<Item>(FriedCheeseCurdsItem.NameId),
                    Duration = 6,
                },
            };
      protected set { }
    }

    public override string ColourBlindTag {
      get => "Cd";
      protected set { }
    }
  }

  public class PotWithCurdsItem : SplittablePotItem {
    public const string NameId = "pot_with_curds";

    public override string UniqueNameID => NameId;

    protected override string DisplayName => "Pot With Curds";

    protected override Color Contents => CheeseCurdsItem.Curd;

    protected override string PortionNameId => CheeseCurdsItem.NameId;

    public override string ColourBlindTag {
      get => "Pd";
      protected set { }
    }
  }

  // IngredientLib's vinegar is a bottle and a pour, so the pot takes the poured serving.
  public class PotWithMilkItem : CustomItemGroup {
    public const string NameId = "pot_with_milk";

    public override string UniqueNameID => NameId;

    private GameObject prefab;

    public override GameObject Prefab {
      get => prefab ?? ( prefab = PotWithSugarItem.BuildPrefab(
          "Pot With Milk", new Color( 0.92f, 0.95f, 0.99f )));
      protected set { }
    }

    public override List<ItemGroup.ItemSet> Sets {
      get => new List<ItemGroup.ItemSet>
      {
                // Pot on its own and mandatory, ingredients together and not.
                new ItemGroup.ItemSet
                {
                    Min = 1,
                    Max = 1,
                    IsMandatory = true,
                    Items = new List<Item> { Gdo.Item(ItemReferences.Pot) },
                },
                new ItemGroup.ItemSet
                {
                    Min = 2,
                    Max = 2,
                    IsMandatory = false,
                    Items = new List<Item>
                    {
                        Gdo.Item(ItemReferences.Milk),
                        Gdo.LibSplit(Gdo.LibKeys.Vinegar),
                    },
                },
            };
      protected set { }
    }

    public override List<Item.ItemProcess> Processes {
      get => new List<Item.ItemProcess>
      {
                new Item.ItemProcess
                {
                    Process = Gdo.Own<Process>(CaramelizeProcess.NameId),
                    Result = Gdo.Own<Item>(PotWithCurdsItem.NameId),
                    Duration = 12,
                },
            };
      protected set { }
    }

    public override Item DisposesTo {
      get => Gdo.Item( ItemReferences.Pot );
      protected set { }
    }

    public override ItemValue ItemValue {
      get => ItemValue.Medium;
      protected set { }
    }

    public override string ColourBlindTag {
      get => "PL";
      protected set { }
    }
  }

  // Exists so a finished poutine does not list Chips - Cooked, which a fries order could take back out.
  public class PoutineBaseItem : CustomItemGroup {
    public const string NameId = "poutine_base";

    public override string UniqueNameID => NameId;

    private GameObject prefab;

    public override GameObject Prefab {
      get => prefab ?? ( prefab = BuildPrefab());
      protected set { }
    }

    // Mandatory is safe on a set of two, since the first merge completes it.
    public override List<ItemGroup.ItemSet> Sets {
      get => new List<ItemGroup.ItemSet>
      {
                new ItemGroup.ItemSet
                {
                    Min = 2,
                    Max = 2,
                    IsMandatory = true,
                    Items = new List<Item>
                    {
                        Gdo.Item(ItemReferences.ChipsCooked),
                        Gdo.Own<Item>(GravyItem.NameId),
                    },
                },
            };
      protected set { }
    }

    // Collapse on assembly, so the chips stop being a component anyone can reclaim.
    public override bool AutoCollapsing {
      get => true;
      protected set { }
    }

    // Not a side; marking a half-made dish mergeable reintroduces the bug this fixes.
    public override bool IsMergeableSide {
      get => false;
      protected set { }
    }

    public override ItemValue ItemValue {
      get => ItemValue.Medium;
      protected set { }
    }

    public override string ColourBlindTag {
      get => "Pg";
      protected set { }
    }

    // Tray, chips and gravy; the curds are the whole difference from a finished one.
    private static GameObject BuildPrefab() {
      var root = Gdo.NewParked( "Poutine - Gravy" );

      var parts = new (string Asset, Color Colour)[]
      {
                ("PoutineTray", new Color(0.78f, 0.18f, 0.16f)),
                ("PoutineFries", new Color(0.93f, 0.72f, 0.28f)),
                ("PoutineGravy", GravyItem.Rich),
      };

      foreach ( var (asset, colour) in parts ) {
        var part = Gdo.CloneFromBundle( asset, asset, colour );
        if ( part != null ) {
          part.transform.SetParent( root.transform, false );
          part.transform.localPosition = Vector3.zero;
        }
      }

      return root;
    }
  }

  // Poutine as the side itself rather than a plate of it.
  public class PoutineItem : CustomItemGroup {
    public const string NameId = "poutine";

    public override string UniqueNameID => NameId;

    private GameObject prefab;

    public override GameObject Prefab {
      get => prefab ?? ( prefab = BuildPrefab());
      protected set { }
    }

    public override List<ItemGroup.ItemSet> Sets {
      get => new List<ItemGroup.ItemSet>
      {
                // Mandatory is safe here, because the first item is already a complete PoutineBaseItem.
                new ItemGroup.ItemSet
                {
                    Min = 2,
                    Max = 2,
                    IsMandatory = true,
                    Items = new List<Item>
                    {
                        Gdo.Own<Item>(PoutineBaseItem.NameId),
                        Gdo.Own<Item>(CheeseCurdsItem.NameId),
                    },
                },
            };
      protected set { }
    }

    // AutoCollapsing leaves nothing for ItemGroupView to match, so each stage carries its own tag.
    public override ItemValue ItemValue {
      get => ItemValue.SideLarge;
      protected set { }
    }

    // The yellow ring the base game draws round a side, paired with ItemGroup.CanContainSide.
    public override bool IsMergeableSide {
      get => true;
      protected set { }
    }

    // Collapse on assembly; see PoutineBaseItem for why this dish is built in two stages.
    public override bool AutoCollapsing {
      get => true;
      protected set { }
    }

    public override string ColourBlindTag {
      get => "Po";
      protected set { }
    }

    // Without this a tray of chips and gravy arrives garnished with cheese that is not there.
    public override void OnRegister( ItemGroup gameDataObject ) {
      base.OnRegister( gameDataObject );

      var built = gameDataObject?.Prefab;
      if ( built == null ) {
        return;
      }

      var view = built.GetComponent<ItemGroupView>() ?? built.AddComponent<ItemGroupView>();
      view.ComponentGroups = new List<ItemGroupView.ComponentGroup>();

      foreach ( var child in new[] { "PoutineTray", "PoutineFries", "PoutineGravy", "PoutineCurds" } ) {
        var target = Gdo.FindChild( built.transform, child );
        if ( target != null ) {
          target.gameObject.SetActive( true );
        }
      }
    }

    // Four meshes because the mod tints whole objects and Simple Flat has one colour per material.
    private static GameObject BuildPrefab() {
      var root = Gdo.NewParked( "Poutine" );

      var parts = new (string Asset, Color Colour)[]
      {
                ("PoutineTray", new Color(0.78f, 0.18f, 0.16f)),
                ("PoutineFries", new Color(0.93f, 0.72f, 0.28f)),
                ("PoutineGravy", GravyItem.Rich),
                ("PoutineCurds", CheeseCurdsItem.Curd),
      };

      foreach ( var (asset, colour) in parts ) {
        var part = Gdo.CloneFromBundle( asset, asset, colour );
        if ( part != null ) {
          part.transform.SetParent( root.transform, false );
          part.transform.localPosition = Vector3.zero;
        }
      }

      return root;
    }
  }

  // Fry once for golden, leave them in and they burn.
  public class FriedCheeseCurdsItem : ScatterToppingItem {
    public const string NameId = "fried_cheese_curds";

    // Browner than a chip, which at chip-gold vanished into the fries.
    public static Color Fried => new Color( 0.66f, 0.42f, 0.17f );

    public override string UniqueNameID => NameId;

    protected override string DisplayName => "Fried Cheese Curds";

    protected override Color Colour => Fried;

    protected override string BundleAsset => "PoutineCurds";

    public override List<Item.ItemProcess> Processes {
      get => new List<Item.ItemProcess>
      {
                new Item.ItemProcess
                {
                    Process = Gdo.FryProcess(),
                    Result = Gdo.Item(ItemReferences.BurnedFood),
                    Duration = 20,
                    IsBad = true,
                },
            };
      protected set { }
    }

    public override string ColourBlindTag {
      get => "Fc";
      protected set { }
    }
  }

  // Double Cheese Poutine: a poutine with fried curds on top of the fresh ones.
  public class DoubleCheesePoutineItem : CustomItemGroup {
    public const string NameId = "double_cheese_poutine";

    public override string UniqueNameID => NameId;

    private GameObject prefab;

    public override GameObject Prefab {
      get => prefab ?? ( prefab = BuildPrefab());
      protected set { }
    }

    public override List<ItemGroup.ItemSet> Sets {
      get => new List<ItemGroup.ItemSet>
      {
                new ItemGroup.ItemSet
                {
                    Min = 2,
                    Max = 2,
                    IsMandatory = false,
                    Items = new List<Item>
                    {
                        Gdo.Own<Item>(PoutineItem.NameId),
                        Gdo.Own<Item>(FriedCheeseCurdsItem.NameId),
                    },
                },
            };
      protected set { }
    }

    public override List<ItemGroupView.ColourBlindLabel> Labels {
      get => new List<ItemGroupView.ColourBlindLabel>
      {
                new ItemGroupView.ColourBlindLabel
                {
                    Item = Gdo.Own<Item>(FriedCheeseCurdsItem.NameId),
                    Text = "Fc",
                },
            };
      protected set { }
    }

    public override ItemValue ItemValue {
      get => ItemValue.SideLarge;
      protected set { }
    }

    public override bool IsMergeableSide {
      get => true;
      protected set { }
    }

    // Collapse to a plain item on assembly, so it stops being a poutine with something on it.
    public override bool AutoCollapsing {
      get => true;
      protected set { }
    }

    private const string PoutineChild = "Poutine";
    private const string FriedChild = "Fried Curds";

    // The whole poutine under one child so the view can switch it as a single component.
    private static GameObject BuildPrefab() {
      var root = Gdo.NewParked( "Double Cheese Poutine" );

      var poutine = Gdo.Own<Item>( PoutineItem.NameId )?.Prefab;
      if ( poutine != null ) {
        var copy = Object.Instantiate( poutine, root.transform );
        copy.name = PoutineChild;
        copy.transform.localPosition = Vector3.zero;
      }

      // The authored 0.05 times the 0.30 the bundle applies.
      var fried = Gdo.CloneFromBundle(
          "PoutineCurds", FriedChild, FriedCheeseCurdsItem.Fried );
      if ( fried != null ) {
        fried.transform.SetParent( root.transform, false );
        fried.transform.localScale = Vector3.one * 1.25f;
        fried.transform.localPosition = new Vector3( 0f, 0.0075f, 0f );
      }

      return root;
    }

    public override void OnRegister( ItemGroup gameDataObject ) {
      base.OnRegister( gameDataObject );

      var built = gameDataObject?.Prefab;
      if ( built == null ) {
        return;
      }

      // No component mappings: AutoCollapsing leaves none, and PerformUpdate would then hide every child.
      var view = built.GetComponent<ItemGroupView>() ?? built.AddComponent<ItemGroupView>();
      view.ComponentGroups = new List<ItemGroupView.ComponentGroup>();

      foreach ( var child in new[] { PoutineChild, FriedChild } ) {
        var target = Gdo.FindChild( built.transform, child );
        if ( target != null ) {
          target.gameObject.SetActive( true );
        }
      }
    }
  }
}
