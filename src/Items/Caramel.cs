// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2026 FireBall1725

using System.Collections.Generic;
using KitchenData;
using KitchenLib.Customs;
using KitchenLib.References;
using UnityEngine;

namespace BeaverTails {
  // Pot and sugar, caramelised for 15s, into eight portions and the pot back.
  public class PotWithSugarItem : CustomItemGroup {
    public const string NameId = "pot_with_sugar";

    public override string UniqueNameID => NameId;

    private GameObject prefab;

    public override GameObject Prefab {
      get => prefab ?? ( prefab = BuildPrefab( "Pot With Sugar", new Color( 0.95f, 0.93f, 0.86f )));
      protected set { }
    }

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
                        Gdo.Item(ItemReferences.Pot),
                        Gdo.Item(ItemReferences.Sugar),
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
                    Result = Gdo.Own<Item>(PotWithCaramelItem.NameId),
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
      get => "PS";
      protected set { }
    }

    // A bare metal pot keeping its own material, with only the contents coloured.
    internal static GameObject BuildPrefab( string name, Color contents ) {
      var pot = Gdo.ClonePrefab( ItemReferences.PotWithOil, name );
      Gdo.AddContents( pot, contents );
      return pot;
    }

    // Same pot, plus a bar above it counting the servings down.
    internal static GameObject BuildSplittablePrefab( string name, Color contents ) {
      var pot = BuildPrefab( name, contents );
      Gdo.AddLevelBar( pot, contents );
      return pot;
    }
  }

  // DisposesTo hands back a bare Pot, so a burned batch costs the contents and not the equipment.
  public class PotWithBurnedItem : CustomItem {
    public const string NameId = "pot_with_burned";

    private static Color Charcoal => new Color( 0.12f, 0.11f, 0.10f );

    public override string UniqueNameID => NameId;

    private GameObject prefab;

    public override GameObject Prefab {
      get => prefab ?? ( prefab = PotWithSugarItem.BuildPrefab( "Pot With Burned", Charcoal ));
      protected set { }
    }

    public override bool IsMergeableSide {
      get => false;
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

    public override ItemCategory ItemCategory {
      get => ItemCategory.Generic;
      protected set { }
    }

    public override string ColourBlindTag {
      get => "Px";
      protected set { }
    }
  }

  // A splittable pot can carry a process as long as it produces a bad result.
  public class PotWithCaramelItem : CustomItem {
    public const string NameId = "pot_with_caramel";

    public static Color Amber => new Color( 0.58f, 0.31f, 0.08f );

    public override string UniqueNameID => NameId;

    private GameObject prefab;

    public override GameObject Prefab {
      get => prefab ?? ( prefab = PotWithSugarItem.BuildSplittablePrefab( "Pot With Caramel", Amber ));
      protected set { }
    }

    // IngredientLib owns caramel, so there is one caramel item rather than two that do not stack.
    public override Item SplitSubItem {
      get => Gdo.Lib( Gdo.LibKeys.Caramel );
      protected set { }
    }

    // Repoint their caramel onto our own cup, the same move MapleSyrupCard makes on their syrup portion.
    public override void OnRegister( Item gameDataObject ) {
      base.OnRegister( gameDataObject );

      var caramel = Gdo.Lib( Gdo.LibKeys.Caramel );
      if ( caramel == null ) {
        return;
      }

      var cup = Gdo.MeasuringCup( "Caramel", Amber, "Ca", "CupDome" );
      if ( cup != null ) {
        caramel.Prefab = cup;
        UnityEngine.Debug.Log( "[BeaverTails] caramel portion is our cup now" );
      }
    }

    // Eight, not fifteen: at fifteen one pot covered most of a service.
    public override int SplitCount {
      get => 8;
      protected set { }
    }

    // Left on the heat it goes black.
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

    // unlike the spread jar, the pot is worth keeping, so hand it back
    public override List<Item> SplitDepletedItems {
      get => new List<Item> { Gdo.Item( ItemReferences.Pot ) };
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

    public override ItemCategory ItemCategory {
      get => ItemCategory.Generic;
      protected set { }
    }

    public override string ColourBlindTag {
      get => "Pc";
      protected set { }
    }
  }

  // Whether you get caramel or filling depends only on whether apple went in before heating.
  public class PotWithAppleItem : CustomItemGroup {
    public const string NameId = "pot_with_apple";

    public override string UniqueNameID => NameId;

    private GameObject prefab;

    public override GameObject Prefab {
      get => prefab ?? ( prefab = PotWithSugarItem.BuildPrefab( "Pot With Apple", Filling ));
      protected set { }
    }

    public static Color Filling => new Color( 0.91f, 0.74f, 0.33f );

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
                        Gdo.Own<Item>(PotWithSugarItem.NameId),
                        Gdo.Item(ItemReferences.AppleSlices),
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
                    Result = Gdo.Own<Item>(PotWithAppleFillingItem.NameId),
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
      get => "PA";
      protected set { }
    }
  }

  // Four servings and the pot back, with no process, or the split interaction vanishes.
  public class PotWithAppleFillingItem : CustomItem {
    public const string NameId = "pot_with_apple_filling";

    public override string UniqueNameID => NameId;

    private GameObject prefab;

    public override GameObject Prefab {
      get => prefab ?? ( prefab = PotWithSugarItem.BuildSplittablePrefab(
          "Pot With Apple Filling", PotWithAppleItem.Filling ));
      protected set { }
    }

    public override Item SplitSubItem {
      get => Gdo.Own<Item>( ApplePieFillingItem.NameId );
      protected set { }
    }

    public override int SplitCount {
      get => 4;
      protected set { }
    }

    // Burns like the caramel pot.
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

    public override List<Item> SplitDepletedItems {
      get => new List<Item> { Gdo.Item( ItemReferences.Pot ) };
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

    public override ItemCategory ItemCategory {
      get => ItemCategory.Generic;
      protected set { }
    }

    public override string ColourBlindTag {
      get => "Pf";
      protected set { }
    }
  }

  // One serving of filling.
  public class ApplePieFillingItem : CustomItem {
    public const string NameId = "apple_pie_filling";

    public override string UniqueNameID => NameId;

    private GameObject prefab;

    // Two prefabs, because the measuring cup can only be cloned in OnRegister and this may not return null.
    public override GameObject Prefab {
      get => prefab ?? ( prefab = Gdo.MeasuringCup(
          "Apple Pie Filling", PotWithAppleItem.Filling, ColourBlindTag, "CupChunks" ));
      protected set { }
    }

    public override void OnRegister( Item gameDataObject ) {
      base.OnRegister( gameDataObject );

      var cup = Gdo.MeasuringCup(
          "Apple Pie Filling", PotWithAppleItem.Filling, ColourBlindTag, "CupChunks" );
      if ( cup == null || gameDataObject == null ) {
        return;
      }

      prefab = cup;
      gameDataObject.Prefab = cup;
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
      get => "PF";
      protected set { }
    }
  }
}
