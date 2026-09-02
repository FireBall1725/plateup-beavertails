// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2026 FireBall1725

using System.Collections.Generic;
using Kitchen;
using KitchenData;
using KitchenLib.Customs;
using KitchenLib.References;
using UnityEngine;

namespace BeaverTails {
  // Syrup taken from IngredientLib: Lib("syrup") is the bottle that carries the provider, LibSplit the serving.

  // A card that adds one thing: syrup a customer may ask for on their tail.
  public class MapleSyrupCard : CustomDish {
    public const string NameId = "recipe_maple_syrup";

    public override string UniqueNameID => NameId;

    public override DishType Type {
      get => DishType.Extra;
      protected set { }
    }

    // see BeaverTailRecipeCard: Generic is not in the dish pool
    public override UnlockGroup UnlockGroup {
      get => UnlockGroup.Dish;
      protected set { }
    }

    // one condiment on an existing plate, so it is the easiest card in the set
    public override int Difficulty {
      get => 1;
      protected set { }
    }

    public override List<Unlock> HardcodedRequirements {
      get => new List<Unlock> { Gdo.Own<Dish>( BeaverTailsDish.NameId ) };
      protected set { }
    }

    // ExtraOrderUnlocks makes a seated customer ask for it; IngredientsUnlocks would put it on the ticket.
    public override HashSet<Dish.IngredientUnlock> ExtraOrderUnlocks {
      get {
        var unlocks = new HashSet<Dish.IngredientUnlock>();

        var syrup = Gdo.Lib( Gdo.LibKeys.Syrup );
        if ( syrup == null ) {
          return unlocks;
        }

        foreach ( var plated in PlatedRecipeItem.AllNameIds ) {
          var menuItem = Gdo.Own<ItemGroup>( plated );
          if ( menuItem == null ) {
            continue;
          }

          unlocks.Add( new Dish.IngredientUnlock {
            MenuItem = menuItem,
            Ingredient = syrup,
          } );
        }

        return unlocks;
      }
      protected set { }
    }

    public override HashSet<Item> MinimumIngredients {
      get {
        // The bottle, not the serving, since the provider hangs off the bottle.
        var ingredients = BeaverTailsDish.BaseIngredients();
        var bottle = Gdo.Lib( Gdo.LibKeys.Syrup );
        if ( bottle != null ) {
          ingredients.Add( bottle );
        }

        return ingredients;
      }
      protected set { }
    }

    public override HashSet<Process> RequiredProcesses {
      get => BeaverTailsDish.BaseProcesses();
      protected set { }
    }

    // Maple amber, for the poured portion.
    private static Color Amber => new Color( 0.76f, 0.44f, 0.10f );

    // Their portion shares the bottle's NameTag, so without this a splash of syrup draws a second bottle.
    public override void OnRegister( Dish gameDataObject ) {
      base.OnRegister( gameDataObject );

      var portion = Gdo.LibSplit( Gdo.LibKeys.Syrup );
      if ( portion == null ) {
        return;
      }

      var cup = Gdo.MeasuringCup( "Syrup Portion", Amber, "Sy", "CupDome" );
      if ( cup != null ) {
        portion.Prefab = cup;
        UnityEngine.Debug.Log( "[BeaverTails] syrup portion is a measuring cup now" );
      }
    }

    private List<(Locale, UnlockInfo)> cachedInfo;

    public override List<(Locale, UnlockInfo)> InfoList {
      get => cachedInfo ?? ( cachedInfo = new List<(Locale, UnlockInfo)>
      {
                (Locale.English, Gdo.Info(
                    "Maple Syrup",
                    "Customers ask for maple syrup while they are eating. Bring them a "
                    + "bottle from the shelf.")),
            } );
      protected set { }
    }
  }

  // Butter and its crate are both base game, so this costs one item and no new source.
  public class MapleButterItem : CustomItemGroup {
    public const string NameId = "maple_butter";

    public static Color Colour => new Color( 0.96f, 0.86f, 0.50f );

    public override string UniqueNameID => NameId;

    private GameObject prefab;

    // The spreads' ramekin with a quenelle instead of a swirl.
    public override GameObject Prefab {
      get => prefab ?? ( prefab = Gdo.Portion(
          "Maple Butter", "SpreadRamekin", "ButterQuenelle", Colour, "MB" ));
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
                        Gdo.Item(ItemReferences.Butter),
                        Gdo.LibSplit(Gdo.LibKeys.Syrup),
                    },
                },
            };
      protected set { }
    }

    public override ItemValue ItemValue {
      get => ItemValue.Small;
      protected set { }
    }

    public override string ColourBlindTag {
      get => "MB";
      protected set { }
    }
  }

  // Maple sugar, boiled rather than stirred; what goes in before the heat decides which pot you get.
  public class PotWithMapleItem : CustomItemGroup {
    public const string NameId = "pot_with_maple";

    public override string UniqueNameID => NameId;

    private GameObject prefab;

    public override GameObject Prefab {
      get => prefab ?? ( prefab = PotWithSugarItem.BuildPrefab(
          "Pot With Maple", MapleSugarItem.Colour ));
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
                        Gdo.Own<Item>(PotWithSugarItem.NameId),
                        Gdo.LibSplit(Gdo.LibKeys.Syrup),
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
                    Result = Gdo.Own<Item>(PotWithMapleSugarItem.NameId),
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
      get => "PM";
      protected set { }
    }
  }

  // Eight servings and the pot back, with no process, or the split interaction goes.
  public class PotWithMapleSugarItem : CustomItem {
    public const string NameId = "pot_with_maple_sugar";

    public override string UniqueNameID => NameId;

    private GameObject prefab;

    public override GameObject Prefab {
      get => prefab ?? ( prefab = PotWithSugarItem.BuildSplittablePrefab(
          "Pot With Maple Sugar", MapleSugarItem.Colour ));
      protected set { }
    }

    public override Item SplitSubItem {
      get => Gdo.Own<Item>( MapleSugarItem.NameId );
      protected set { }
    }

    public override int SplitCount {
      get => 8;
      protected set { }
    }

    // Burns like the caramel pot, same duration, same shared burned-pot result.
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
      get => "Pm";
      protected set { }
    }
  }

  // One serving of maple sugar.
  public class MapleSugarItem : CustomItem {
    public const string NameId = "maple_sugar";

    public static Color Colour => new Color( 0.71f, 0.45f, 0.15f );

    public override string UniqueNameID => NameId;

    private GameObject prefab;

    public override GameObject Prefab {
      get => prefab ?? ( prefab = Gdo.ClonePrefab(
          ItemReferences.Sugar, "Maple Sugar", tint: Colour ));
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
      get => "MS";
      protected set { }
    }
  }
}
