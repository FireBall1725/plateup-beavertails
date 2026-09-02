// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2026 FireBall1725

using System.Collections.Generic;
using KitchenData;
using KitchenLib.Customs;

namespace BeaverTails {
  // Recipe cards, named after the real menu at beavertails.com.
  public abstract class BeaverTailRecipeCard : CustomDish {
    protected abstract string CardName { get; }

    protected abstract string CardDescription { get; }

    protected abstract string PlatedNameId { get; }

    // the ingredients this recipe adds that the base dish does not already provide
    protected abstract IEnumerable<Item> NewIngredients { get; }

    // Processes beyond the base dish's Knead and FryMe.
    protected virtual IEnumerable<Process> NewProcesses => new Process[0];

    public override DishType Type {
      get => DishType.Extra;
      protected set { }
    }

    // CustomUnlock defaults to Generic, which sits outside the dish pool and is never offered.
    public override UnlockGroup UnlockGroup {
      get => UnlockGroup.Dish;
      protected set { }
    }

    // Stars on the card, 0-5.
    protected abstract int RecipeDifficulty { get; }

    public override int Difficulty {
      get => RecipeDifficulty;
      protected set { }
    }

    // Harder recipes send fewer customers.
    public override DishCustomerChange CustomerMultiplier {
      get {
        if ( RecipeDifficulty >= 5 ) {
          return DishCustomerChange.LargeDecrease;
        }

        return RecipeDifficulty >= 3 ? DishCustomerChange.SmallDecrease : DishCustomerChange.None;
      }
      protected set { }
    }

    // only offered when you are already running Beaver Tails
    public override List<Unlock> HardcodedRequirements {
      get => new List<Unlock> { Gdo.Own<Dish>( BeaverTailsDish.NameId ) };
      protected set { }
    }

    public override List<Dish.MenuItem> ResultingMenuItems {
      get {
        var plated = Gdo.Own<Item>( PlatedNameId );
        if ( plated == null ) {
          return new List<Dish.MenuItem>();
        }

        return new List<Dish.MenuItem>
        {
                    new Dish.MenuItem
                    {
                        Item = plated,
                        Phase = MenuPhase.Main,
                        Weight = 1f,
                    },
                };
      }
      protected set { }
    }

    // Names the source item rather than the prepared form, because the provider dispenses the raw one.
    public override HashSet<Item> MinimumIngredients {
      get {
        var ingredients = BeaverTailsDish.BaseIngredients();
        foreach ( var extra in NewIngredients ) {
          if ( extra != null ) {
            ingredients.Add( extra );
          }
        }

        return ingredients;
      }
      protected set { }
    }

    public override HashSet<Process> RequiredProcesses {
      get {
        var processes = BeaverTailsDish.BaseProcesses();
        foreach ( var extra in NewProcesses ) {
          if ( extra != null ) {
            processes.Add( extra );
          }
        }

        return processes;
      }
      protected set { }
    }

    private List<(Locale, UnlockInfo)> cachedInfo;

    public override List<(Locale, UnlockInfo)> InfoList {
      get => cachedInfo ?? ( cachedInfo = new List<(Locale, UnlockInfo)>
      {
                (Locale.English, Gdo.Info(CardName, CardDescription)),
            } );
      protected set { }
    }
  }

  public class KillaloeSunriseCard : BeaverTailRecipeCard {
    public const string NameId = "recipe_killaloe_sunrise";

    public override string UniqueNameID => NameId;

    protected override int RecipeDifficulty => 3;

    protected override string CardName => "Killaloe Sunrise";

    protected override string CardDescription =>
        "Cinnamon and sugar, served with a slice of lemon.";

    protected override string PlatedNameId => BeaverTailKillaloePlatedItem.NameId;

    protected override IEnumerable<Item> NewIngredients =>
        new[] { Gdo.Item( KitchenLib.References.ItemReferences.Lemon ) };
  }

  public class HazelAmourCard : BeaverTailRecipeCard {
    public const string NameId = "recipe_hazel_amour";

    public override string UniqueNameID => NameId;

    protected override int RecipeDifficulty => 3;

    protected override string CardName => "Hazel Amour";

    protected override string CardDescription =>
        "Chocolate hazelnut spread with a dusting of icing sugar.";

    protected override string PlatedNameId => BeaverTailHazelAmourPlatedItem.NameId;

    // the jar, not the portion: the jar is what has a provider
    protected override IEnumerable<Item> NewIngredients =>
        new[] { Gdo.Own<Item>( HazelnutJarItem.NameId ) };
  }

  public class BananaramaCard : BeaverTailRecipeCard {
    public const string NameId = "recipe_bananarama";

    public override string UniqueNameID => NameId;

    protected override int RecipeDifficulty => 4;

    protected override string CardName => "Bananarama";

    protected override string CardDescription =>
        "Chocolate hazelnut spread topped with fresh banana slices.";

    protected override string PlatedNameId => BeaverTailBananaramaPlatedItem.NameId;

    // Listing the jar on both cards means either works alone and the second adds no rack.
    protected override IEnumerable<Item> NewIngredients =>
        new[] { Gdo.Lib( Gdo.LibKeys.Banana ), Gdo.Own<Item>( HazelnutJarItem.NameId ) };
  }

  public class ApplePieCard : BeaverTailRecipeCard {
    public const string NameId = "recipe_apple_pie";

    public override string UniqueNameID => NameId;

    protected override int RecipeDifficulty => 5;

    protected override string CardName => "Apple Pie";

    protected override string CardDescription =>
        "Apple pie filling with a caramel drizzle, both stewed in a pot on the hob.";

    protected override string PlatedNameId => BeaverTailApplePiePlatedItem.NameId;

    // An apple to chop and a pot to cook in; sugar is already in the base set.
    protected override IEnumerable<Item> NewIngredients => new[]
    {
            Gdo.Item(KitchenLib.References.ItemReferences.Apple),
            Gdo.Item(KitchenLib.References.ItemReferences.Pot),
        };

    // Caramelise is hob-only, because the fryer also provides Cook.
    protected override IEnumerable<Process> NewProcesses => new[]
    {
            Gdo.Own<Process>(CaramelizeProcess.NameId),
        };
  }

  public class TripleTripCard : BeaverTailRecipeCard {
    public const string NameId = "recipe_triple_trip";

    public override string UniqueNameID => NameId;

    protected override int RecipeDifficulty => 4;

    protected override string CardName => "Triple Trip";

    protected override string CardDescription =>
        "Chocolate hazelnut spread, peanut butter, and a scatter of Reese's Pieces.";

    protected override string PlatedNameId => BeaverTailTripleTripPlatedItem.NameId;

    // The hazelnut rack is listed so this card stands alone.
    protected override IEnumerable<Item> NewIngredients => new[]
    {
            Gdo.Own<Item>(HazelnutJarItem.NameId),
            Gdo.Own<Item>(PeanutJarItem.NameId),
            Gdo.Own<Item>(ReesesPiecesItem.NameId),
        };
  }

  public class AvalancheCard : BeaverTailRecipeCard {
    public const string NameId = "recipe_avalanche";

    public override string UniqueNameID => NameId;

    protected override int RecipeDifficulty => 4;

    protected override string CardName => "Avalanche";

    protected override string CardDescription =>
        "Cheesecake spread, Skor bits, and a caramel drizzle.";

    protected override string PlatedNameId => BeaverTailAvalanchePlatedItem.NameId;

    // The caramel station is shared with Apple Pie.
    protected override IEnumerable<Item> NewIngredients => new[]
    {
            Gdo.Own<Item>(CheesecakeJarItem.NameId),
            Gdo.Own<Item>(SkorBitsItem.NameId),
            Gdo.Item(KitchenLib.References.ItemReferences.Pot),
        };

    protected override IEnumerable<Process> NewProcesses => new[]
    {
            Gdo.Own<Process>(CaramelizeProcess.NameId),
        };
  }

  public class MehpleCard : BeaverTailRecipeCard {
    public const string NameId = "recipe_mehple";

    public override string UniqueNameID => NameId;

    protected override int RecipeDifficulty => 4;

    protected override string CardName => "mEHple";

    protected override string CardDescription =>
        "Maple butter, and maple sugar boiled in a pot. Whip butter with syrup; cook "
        + "sugar and syrup on the hob for eight servings.";

    protected override string PlatedNameId => BeaverTailMehplePlatedItem.NameId;

    // All shared with other cards, so stacking them adds no second dispenser.
    protected override IEnumerable<Item> NewIngredients => new[]
    {
            Gdo.Item(KitchenLib.References.ItemReferences.Butter),
            Gdo.Lib(Gdo.LibKeys.Syrup),
            Gdo.Item(KitchenLib.References.ItemReferences.Pot),
        };

    protected override IEnumerable<Process> NewProcesses => new[]
    {
            Gdo.Own<Process>(CaramelizeProcess.NameId),
        };
  }

  public class PistachOhCard : BeaverTailRecipeCard {
    public const string NameId = "recipe_pistachoh";

    public override string UniqueNameID => NameId;

    protected override int RecipeDifficulty => 4;

    protected override string CardName => "Pistach-OH";

    protected override string CardDescription =>
        "Pistachio spread and honey, finished with chopped pistachios.";

    protected override string PlatedNameId => BeaverTailPistachOhPlatedItem.NameId;

    // Honey is IngredientLib's bottle, the half of the pair that carries a dispenser.
    protected override IEnumerable<Item> NewIngredients => new[]
    {
            Gdo.Own<Item>(PistachioJarItem.NameId),
            Gdo.Own<Item>(PistachioCrumbsItem.NameId),
            Gdo.Lib(Gdo.LibKeys.Honey),
        };
  }

  public class StrawberryCheesecakeCard : BeaverTailRecipeCard {
    public const string NameId = "recipe_strawberry_cheesecake";

    public override string UniqueNameID => NameId;

    protected override int RecipeDifficulty => 4;

    protected override string CardName => "Strawberry Cheesecake";

    protected override string CardDescription =>
        "Cheesecake spread and strawberry syrup, finished with crushed crackers.";

    protected override string PlatedNameId => BeaverTailStrawberryCheesecakePlatedItem.NameId;

    // The syrup and crackers are the container rather than the serving, because that carries a provider.
    protected override IEnumerable<Item> NewIngredients => new[]
    {
            Gdo.Own<Item>(CheesecakeJarItem.NameId),
            Gdo.Item(KitchenLib.References.ItemReferences.SundaeSyrupStrawberry),
            Gdo.Lib(Gdo.LibKeys.Crackers),
        };
  }

  public class CocoVanilCard : BeaverTailRecipeCard {
    public const string NameId = "recipe_coco_vanil";

    public override string UniqueNameID => NameId;

    protected override int RecipeDifficulty => 5;

    protected override string CardName => "Coco Vanil";

    protected override string CardDescription =>
        "Vanilla icing and crushed cookies, finished with a chocolate drizzle.";

    protected override string PlatedNameId => BeaverTailCocoVanilPlatedItem.NameId;

    // The chocolate syrup is listed as the bottle so it brings a dispenser.
    protected override IEnumerable<Item> NewIngredients => new[]
    {
            Gdo.Own<Item>(VanillaIcingJarItem.NameId),
            Gdo.Own<Item>(CrushedOreoItem.NameId),
            Gdo.Item(KitchenLib.References.ItemReferences.SundaeSyrupChocolate),
        };
  }

  public class BrwownieCard : BeaverTailRecipeCard {
    public const string NameId = "recipe_brwownie";

    public override string UniqueNameID => NameId;

    protected override int RecipeDifficulty => 5;

    protected override string CardName => "BrWOWnie";

    protected override string CardDescription =>
        "Hazelnut spread and a piece of oven-baked brownie, finished with white chocolate chunks.";

    protected override string PlatedNameId => BeaverTailBrwowniePlatedItem.NameId;

    // Read off the base game's own Brownies dish at runtime; Flour and Sugar are already in the base set.
    protected override IEnumerable<Item> NewIngredients => new[]
    {
            Gdo.Own<Item>(HazelnutJarItem.NameId),
            Gdo.Own<Item>(WhiteChocolateChunksItem.NameId),
            Gdo.Item(KitchenLib.References.ItemReferences.Chocolate),
            Gdo.Item(KitchenLib.References.ItemReferences.BrownieTray),
            Gdo.Item(KitchenLib.References.ItemReferences.MixingBowlEmpty),
            Gdo.Item(KitchenLib.References.ItemReferences.Egg),
        };

    // RequireOven rather than a custom Bake, because the Fryer also provides Cook.
    protected override IEnumerable<Process> NewProcesses => new[]
    {
            Gdo.Process(KitchenLib.References.ProcessReferences.RequireOven),
        };
  }

  // Chips as a side course, gated behind the Beaver Tails dish since the base game ships its own.
  public class FriesCard : CustomDish {
    public const string NameId = "recipe_fries";

    public override string UniqueNameID => NameId;

    public override DishType Type {
      get => DishType.Side;
      protected set { }
    }

    public override UnlockGroup UnlockGroup {
      get => UnlockGroup.Dish;
      protected set { }
    }

    public override int Difficulty {
      get => 2;
      protected set { }
    }

    public override List<Unlock> HardcodedRequirements {
      get => new List<Unlock> { Gdo.Own<Dish>( BeaverTailsDish.NameId ) };
      protected set { }
    }

    public override List<Dish.MenuItem> ResultingMenuItems {
      get {
        var chips = Gdo.Item( KitchenLib.References.ItemReferences.ChipsCooked );
        if ( chips == null ) {
          return new List<Dish.MenuItem>();
        }

        return new List<Dish.MenuItem>
        {
                    new Dish.MenuItem
                    {
                        Item = chips,
                        Phase = MenuPhase.Side,
                        Weight = 1f,
                    },
                };
      }
      protected set { }
    }

    // Only what the side itself needs; Chop and Cook arrive with the base dish.
    public override HashSet<Item> MinimumIngredients {
      get {
        var potato = Gdo.Item( KitchenLib.References.ItemReferences.Potato );
        return potato == null
            ? new HashSet<Item>()
            : new HashSet<Item> { potato };
      }
      protected set { }
    }

    public override HashSet<Process> RequiredProcesses {
      get => new HashSet<Process> { Gdo.Process( KitchenLib.References.ProcessReferences.Chop ) };
      protected set { }
    }

    private List<(Locale, UnlockInfo)> cachedInfo;

    public override List<(Locale, UnlockInfo)> InfoList {
      get => cachedInfo ?? ( cachedInfo = new List<(Locale, UnlockInfo)>
      {
                (Locale.English, Gdo.Info(
                    "Fries",
                    "A side of fries. Chop a potato and fry the pieces.")),
            } );
      protected set { }
    }
  }

  // A side course built from three of our own pot chains.
  public class PoutineCard : CustomDish {
    public const string NameId = "recipe_poutine";

    public override string UniqueNameID => NameId;

    public override DishType Type {
      get => DishType.Side;
      protected set { }
    }

    public override UnlockGroup UnlockGroup {
      get => UnlockGroup.Dish;
      protected set { }
    }

    // The most work any card in the mod asks for.
    public override int Difficulty {
      get => 5;
      protected set { }
    }

    public override DishCustomerChange CustomerMultiplier {
      get => DishCustomerChange.LargeDecrease;
      protected set { }
    }

    public override List<Unlock> HardcodedRequirements {
      get => new List<Unlock> { Gdo.Own<Dish>( BeaverTailsDish.NameId ) };
      protected set { }
    }

    public override List<Dish.MenuItem> ResultingMenuItems {
      get {
        var poutine = Gdo.Own<Item>( PoutineItem.NameId );
        if ( poutine == null ) {
          return new List<Dish.MenuItem>();
        }

        return new List<Dish.MenuItem>
        {
                    new Dish.MenuItem
                    {
                        Item = poutine,
                        Phase = MenuPhase.Side,
                        Weight = 1f,
                    },
                };
      }
      protected set { }
    }

    // Flour is listed even though the base dish brings it, because a side card stands on its own.
    public override HashSet<Item> MinimumIngredients {
      get {
        var ingredients = new HashSet<Item>
        {
                    Gdo.Item(KitchenLib.References.ItemReferences.Potato),
                    Gdo.Item(KitchenLib.References.ItemReferences.Pot),
                    Gdo.Item(KitchenLib.References.ItemReferences.Meat),
                    Gdo.Item(KitchenLib.References.ItemReferences.Butter),
                    Gdo.Item(KitchenLib.References.ItemReferences.Flour),
                    Gdo.Item(KitchenLib.References.ItemReferences.Milk),
                    Gdo.Lib(Gdo.LibKeys.Vinegar),
                };

        ingredients.Remove( null );
        return ingredients;
      }
      protected set { }
    }

    // Cook comes from the fryer the base dish already brings.
    public override HashSet<Process> RequiredProcesses {
      get => new HashSet<Process>
      {
                Gdo.Process(KitchenLib.References.ProcessReferences.Chop),
                Gdo.Own<Process>(CaramelizeProcess.NameId),
            };
      protected set { }
    }

    private List<(Locale, UnlockInfo)> cachedInfo;

    public override List<(Locale, UnlockInfo)> InfoList {
      get => cachedInfo ?? ( cachedInfo = new List<(Locale, UnlockInfo)>
      {
                (Locale.English, Gdo.Info(
                    "Poutine",
                    "Fries under gravy and cheese curds.",
                    "Ask for it with a beaver tail and nobody will blink.")),
            } );
      protected set { }
    }
  }

  // Double Cheese Poutine: everything Poutine needs, plus a trip through the fryer.
  public class DoubleCheesePoutineCard : CustomDish {
    public const string NameId = "recipe_double_cheese_poutine";

    public override string UniqueNameID => NameId;

    public override DishType Type {
      get => DishType.Side;
      protected set { }
    }

    public override UnlockGroup UnlockGroup {
      get => UnlockGroup.Dish;
      protected set { }
    }

    public override int Difficulty {
      get => 5;
      protected set { }
    }

    public override DishCustomerChange CustomerMultiplier {
      get => DishCustomerChange.LargeDecrease;
      protected set { }
    }

    // Poutine first; everything this adds is built on top of that dish.
    public override List<Unlock> HardcodedRequirements {
      get => new List<Unlock>
      {
                Gdo.Own<Dish>(BeaverTailsDish.NameId),
                Gdo.Own<Dish>(PoutineCard.NameId),
            };
      protected set { }
    }

    public override List<Dish.MenuItem> ResultingMenuItems {
      get {
        var doubled = Gdo.Own<Item>( DoubleCheesePoutineItem.NameId );
        if ( doubled == null ) {
          return new List<Dish.MenuItem>();
        }

        return new List<Dish.MenuItem>
        {
                    new Dish.MenuItem
                    {
                        Item = doubled,
                        Phase = MenuPhase.Side,
                        Weight = 1f,
                    },
                };
      }
      protected set { }
    }

    // The same set Poutine needs, since this is a second cook of what the kitchen already makes.
    public override HashSet<Item> MinimumIngredients {
      get {
        var ingredients = new HashSet<Item>
        {
                    Gdo.Item(KitchenLib.References.ItemReferences.Potato),
                    Gdo.Item(KitchenLib.References.ItemReferences.Pot),
                    Gdo.Item(KitchenLib.References.ItemReferences.Meat),
                    Gdo.Item(KitchenLib.References.ItemReferences.Butter),
                    Gdo.Item(KitchenLib.References.ItemReferences.Flour),
                    Gdo.Item(KitchenLib.References.ItemReferences.Milk),
                    Gdo.Lib(Gdo.LibKeys.Vinegar),
                };

        ingredients.Remove( null );
        return ingredients;
      }
      protected set { }
    }

    public override HashSet<Process> RequiredProcesses {
      get => new HashSet<Process>
      {
                Gdo.Process(KitchenLib.References.ProcessReferences.Chop),
                Gdo.Own<Process>(CaramelizeProcess.NameId),
                Gdo.FryProcess(),
            };
      protected set { }
    }

    private List<(Locale, UnlockInfo)> cachedInfo;

    public override List<(Locale, UnlockInfo)> InfoList {
      get => cachedInfo ?? ( cachedInfo = new List<(Locale, UnlockInfo)>
      {
                (Locale.English, Gdo.Info(
                    "Double Cheese Poutine",
                    "A poutine, with fried cheese curds on top of the fresh ones.",
                    "Nobody has ever finished one.")),
            } );
      protected set { }
    }
  }
}
