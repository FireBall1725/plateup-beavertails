// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2026 FireBall1725

using System.Collections.Generic;
using Kitchen;
using KitchenData;
using KitchenLib.Customs;
using KitchenLib.References;
using UnityEngine;

namespace BeaverTails {
  // The plate gets its own mandatory set and the food set does not, or ordering breaks.
  public abstract class PlatedRecipeItem : CustomItemGroup {
    protected abstract string RecipeNameId { get; }

    protected abstract string PlateName { get; }

    protected abstract string RecipeLabel { get; }

    private GameObject prefab;

    public override GameObject Prefab {
      get => prefab ?? ( prefab = BuildPrefab());
      protected set { }
    }

    // The game's fixed ladder: Small 3, Medium 5, MediumLarge 6, Large 8, ExtraLarge 10.
    protected virtual ItemValue Value => ItemValue.Medium;

    public override ItemValue ItemValue {
      get => Value;
      protected set { }
    }

    public override List<ItemGroup.ItemSet> Sets {
      get => new List<ItemGroup.ItemSet>
      {
                new ItemGroup.ItemSet
                {
                    Min = 1,
                    Max = 1,
                    IsMandatory = true,
                    Items = new List<Item> { Gdo.Item(ItemReferences.Plate) },
                },
                new ItemGroup.ItemSet
                {
                    Min = 1,
                    Max = 1,
                    IsMandatory = false,
                    Items = new List<Item> { Gdo.Own<Item>(RecipeNameId) },
                },

            };
      protected set { }
    }

    // Every plated recipe, so the syrup card offers itself on all of them without a second list.
    public static readonly string[] AllNameIds =
    {
            BeaverTailClassicPlatedItem.NameId,
            BeaverTailKillaloePlatedItem.NameId,
            BeaverTailHazelAmourPlatedItem.NameId,
            BeaverTailBananaramaPlatedItem.NameId,
            BeaverTailApplePiePlatedItem.NameId,
            BeaverTailTripleTripPlatedItem.NameId,
            BeaverTailAvalanchePlatedItem.NameId,
            BeaverTailMehplePlatedItem.NameId,
            BeaverTailPistachOhPlatedItem.NameId,
            BeaverTailStrawberryCheesecakePlatedItem.NameId,
            BeaverTailCocoVanilPlatedItem.NameId,
            BeaverTailBrwowniePlatedItem.NameId,
        };

    // The other half of the pair with Item.IsMergeableSide.
    public override bool CanContainSide {
      get => true;
      protected set { }
    }

    // so a finished plate goes into the washing-up loop like any other
    public override Item DirtiesTo {
      get => Gdo.Item( ItemReferences.PlateDirty );
      protected set { }
    }

    // The bin reads DisposesTo and nothing else; DirtiesTo only covers a customer finishing.
    // Without this, binning a plated tail eats the plate along with the food.
    public override Item DisposesTo {
      get => Gdo.Item( ItemReferences.PlateDirty );
      protected set { }
    }

    // The view renders the components and is where KitchenLib writes the colourblind labels.
    public override bool AutoSetupItemGroupView {
      get => true;
      protected set { }
    }

    public override List<ItemGroupView.ColourBlindLabel> Labels {
      get => new List<ItemGroupView.ColourBlindLabel>
      {
                new ItemGroupView.ColourBlindLabel
                {
                    Item = Gdo.Own<Item>(RecipeNameId),
                    Text = RecipeLabel,
                },
            };
      protected set { }
    }

    // ItemGroupView.AddComponent returns early for an unmapped component, leaving a side servable but invisible.
    private static IEnumerable<(Item Item, string Child, float Scale)> PlatableSides() {
      yield return (Gdo.Own<Item>( PoutineItem.NameId ), "Side Poutine", PoutineScale);
      yield return (Gdo.Own<Item>( DoubleCheesePoutineItem.NameId ), "Side Double Poutine", PoutineScale);
      yield return (Gdo.Item( ItemReferences.ChipsCooked ), "Side Chips", 1f);
    }

    // Scales are read back from each prefab's bounds; the offsets are by eye.
    private const float PoutineScale = 0.88f;
    private static readonly Vector3 SideOffset = new Vector3( 0.21f, 0.05f, -0.04f );

    // Without a mapping the view has nothing registered and the plate renders bare.
    public override void OnRegister( ItemGroup gameDataObject ) {
      base.OnRegister( gameDataObject );

      var plate = gameDataObject?.Prefab;
      if ( plate == null ) {
        return;
      }

      var view = plate.GetComponent<ItemGroupView>() ?? plate.AddComponent<ItemGroupView>();

      var tail = plate.transform.Find( PlatedTailChild );
      var recipe = Gdo.Own<Item>( RecipeNameId );
      if ( tail == null || recipe == null ) {
        return;
      }

      var groups = new List<ItemGroupView.ComponentGroup>
      {
                new ItemGroupView.ComponentGroup
                {
                    Item = recipe,
                    GameObject = tail.gameObject,
                },
            };

      // The base game's Plate prefab carries no ItemGroupView, so this list starts empty.
      foreach ( var side in PlatableSides()) {
        if ( side.Item?.Prefab == null ) {
          continue;
        }

        var existing = plate.transform.Find( side.Child );
        var obj = existing != null
            ? existing.gameObject
            : Gdo.Unlabel( Object.Instantiate( side.Item.Prefab, plate.transform ));

        obj.name = side.Child;
        obj.transform.localPosition = SideOffset;
        obj.transform.localScale = Vector3.one * side.Scale;
        obj.SetActive( true );

        groups.Add( new ItemGroupView.ComponentGroup {
          Item = side.Item,
          GameObject = obj,
        } );
      }

      view.ComponentGroups = groups;

      // Left on: ItemGroupView only hides components on its first in-game update.
      tail.gameObject.SetActive( true );
    }

    private const string PlatedTailChild = "Plated Tail";

    // A plate with this recipe's own finished tail sitting on it.
    private GameObject BuildPrefab() {
      var plate = Gdo.ClonePrefab( ItemReferences.Plate, PlateName );
      if ( plate == null ) {
        return null;
      }

      var recipe = Gdo.Own<Item>( RecipeNameId );
      if ( recipe?.Prefab != null ) {
        var tail = Gdo.Unlabel( Object.Instantiate( recipe.Prefab, plate.transform ));
        tail.name = PlatedTailChild;
        tail.transform.localPosition = new Vector3( 0f, 0.06f, 0f );
      }

      return plate;
    }
  }

  public class BeaverTailClassicPlatedItem : PlatedRecipeItem {
    public const string NameId = "beavertail_classic_plated";

    public override string UniqueNameID => NameId;

    protected override string RecipeNameId => BeaverTailClassicItem.NameId;

    protected override string PlateName => "The Classic - Plated";

    protected override string RecipeLabel => "CS";
  }

  public class BeaverTailKillaloePlatedItem : PlatedRecipeItem {
    protected override ItemValue Value => ItemValue.MediumLarge;

    public const string NameId = "beavertail_killaloe_plated";

    public override string UniqueNameID => NameId;

    protected override string RecipeNameId => BeaverTailKillaloeItem.NameId;

    protected override string PlateName => "Killaloe Sunrise - Plated";

    protected override string RecipeLabel => "KS";
  }

  public class BeaverTailPistachOhPlatedItem : PlatedRecipeItem {
    protected override ItemValue Value => ItemValue.Large;

    public const string NameId = "beavertail_pistachoh_plated";

    public override string UniqueNameID => NameId;

    protected override string RecipeNameId => BeaverTailPistachOhItem.NameId;

    protected override string PlateName => "Pistach-OH - Plated";

    protected override string RecipeLabel => "PO";
  }

  public class BeaverTailStrawberryCheesecakePlatedItem : PlatedRecipeItem {
    protected override ItemValue Value => ItemValue.Large;

    public const string NameId = "beavertail_strawberry_cheesecake_plated";

    public override string UniqueNameID => NameId;

    protected override string RecipeNameId => BeaverTailStrawberryCheesecakeItem.NameId;

    protected override string PlateName => "Strawberry Cheesecake - Plated";

    protected override string RecipeLabel => "SC";
  }

  public class BeaverTailCocoVanilPlatedItem : PlatedRecipeItem {
    protected override ItemValue Value => ItemValue.ExtraLarge;

    public const string NameId = "beavertail_coco_vanil_plated";

    public override string UniqueNameID => NameId;

    protected override string RecipeNameId => BeaverTailCocoVanilItem.NameId;

    protected override string PlateName => "Coco Vanil - Plated";

    protected override string RecipeLabel => "CV";
  }

  public class BeaverTailBrwowniePlatedItem : PlatedRecipeItem {
    protected override ItemValue Value => ItemValue.ExtraLarge;

    public const string NameId = "beavertail_brwownie_plated";

    public override string UniqueNameID => NameId;

    protected override string RecipeNameId => BeaverTailBrwownieItem.NameId;

    protected override string PlateName => "BrWOWnie - Plated";

    protected override string RecipeLabel => "WW";
  }

  public class BeaverTailHazelAmourPlatedItem : PlatedRecipeItem {
    protected override ItemValue Value => ItemValue.MediumLarge;

    public const string NameId = "beavertail_hazel_amour_plated";

    public override string UniqueNameID => NameId;

    protected override string RecipeNameId => BeaverTailHazelAmourItem.NameId;

    protected override string PlateName => "Hazel Amour - Plated";

    protected override string RecipeLabel => "HA";
  }

  public class BeaverTailBananaramaPlatedItem : PlatedRecipeItem {
    protected override ItemValue Value => ItemValue.Large;

    public const string NameId = "beavertail_bananarama_plated";

    public override string UniqueNameID => NameId;

    protected override string RecipeNameId => BeaverTailBananaramaItem.NameId;

    protected override string PlateName => "Bananarama - Plated";

    protected override string RecipeLabel => "BR";
  }

  public class BeaverTailApplePiePlatedItem : PlatedRecipeItem {
    protected override ItemValue Value => ItemValue.ExtraLarge;

    public const string NameId = "beavertail_apple_pie_plated";

    public override string UniqueNameID => NameId;

    protected override string RecipeNameId => BeaverTailApplePieItem.NameId;

    protected override string PlateName => "Apple Pie - Plated";

    protected override string RecipeLabel => "AP";
  }

  public class BeaverTailTripleTripPlatedItem : PlatedRecipeItem {
    protected override ItemValue Value => ItemValue.Large;

    public const string NameId = "beavertail_triple_trip_plated";

    public override string UniqueNameID => NameId;

    protected override string RecipeNameId => BeaverTailTripleTripItem.NameId;

    protected override string PlateName => "Triple Trip - Plated";

    protected override string RecipeLabel => "TT";
  }

  public class BeaverTailAvalanchePlatedItem : PlatedRecipeItem {
    protected override ItemValue Value => ItemValue.Large;

    public const string NameId = "beavertail_avalanche_plated";

    public override string UniqueNameID => NameId;

    protected override string RecipeNameId => BeaverTailAvalancheItem.NameId;

    protected override string PlateName => "Avalanche - Plated";

    protected override string RecipeLabel => "Av";
  }

  public class BeaverTailMehplePlatedItem : PlatedRecipeItem {
    protected override ItemValue Value => ItemValue.Large;

    public const string NameId = "beavertail_mehple_plated";

    public override string UniqueNameID => NameId;

    protected override string RecipeNameId => BeaverTailMehpleItem.NameId;

    protected override string PlateName => "mEHple - Plated";

    protected override string RecipeLabel => "Mp";
  }
}
