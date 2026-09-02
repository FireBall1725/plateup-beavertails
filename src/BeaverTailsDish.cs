// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2026 FireBall1725

using System.Collections.Generic;
using KitchenData;
using KitchenLib.Customs;
using KitchenLib.References;
using UnityEngine;

namespace BeaverTails {
  // The base dish, which is The Classic: cinnamon and sugar, with every other product arriving
  // as its own recipe card.
  public class BeaverTailsDish : CustomDish {
    public const string NameId = "beavertails_poc_dish";

    public override string UniqueNameID => NameId;

    private GameObject iconPrefab;
    private GameObject displayPrefab;

    public override GameObject IconPrefab {
      get => iconPrefab ?? ( iconPrefab = Gdo.CloneFromBundle(
          "BeaverTail", "Beaver Tails Card Icon", BeaverTailClassicItem.Colour ));
      protected set { }
    }

    public override GameObject DisplayPrefab {
      get => displayPrefab ?? ( displayPrefab = Gdo.CloneFromBundle(
          "BeaverTail", "Beaver Tails Card Display", BeaverTailClassicItem.Colour ));
      protected set { }
    }

    private List<(Locale, UnlockInfo)> cachedInfo;

    public override List<(Locale, UnlockInfo)> InfoList {
      get => cachedInfo ?? ( cachedInfo = new List<(Locale, UnlockInfo)>
      {
                (Locale.English, Gdo.Info(
                    "Beaver Tails",
                    "Fried dough pastry, dusted with cinnamon and sugar.",
                    "Best eaten standing on a frozen canal.")),
            } );
      protected set { }
    }

    // Recipe text goes through RegisterRecipeTextSystem; overriding Recipe hard-crashes KitchenLib.

    public override DishType Type {
      get => DishType.Main;
      protected set { }
    }

    // Same group as the base game's headline dishes; the CustomUnlock default of Generic
    // leaves the dish outside the pool entirely.
    public override UnlockGroup UnlockGroup {
      get => UnlockGroup.Dish;
      protected set { }
    }

    public override int Difficulty {
      get => 2;
      protected set { }
    }

    // FilterBasic drops every Dish candidate not named in AllowedFoods, and the run dish is always held.
    public override bool BlocksAllOtherFood {
      get => true;
      protected set { }
    }

    // The allow-list the flag above reads, so a card added to Mod.cs and forgotten here will
    // never be offered.
    public override List<Unlock> AllowedFoods {
      get {
        var foods = new List<Unlock>();
        foreach ( var nameId in AllowedCardIds ) {
          var card = Gdo.Own<Dish>( nameId );
          if ( card != null ) {
            foods.Add( card );
          }
        }

        return foods;
      }
      protected set { }
    }

    // Kept next to the flag rather than derived from Mod.cs, because registration order is
    // about dependencies and this is about what a customer can be offered.
    private static readonly string[] AllowedCardIds =
    {
            KillaloeSunriseCard.NameId,
            HazelAmourCard.NameId,
            BananaramaCard.NameId,
            ApplePieCard.NameId,
            TripleTripCard.NameId,
            AvalancheCard.NameId,
            MehpleCard.NameId,
            PistachOhCard.NameId,
            StrawberryCheesecakeCard.NameId,
            CocoVanilCard.NameId,
            BrwownieCard.NameId,
            FriesCard.NameId,
            PoutineCard.NameId,
            DoubleCheesePoutineCard.NameId,
            MapleSyrupCard.NameId,
        };

    public override bool IsAvailableAsLobbyOption {
      get => true;
      protected set { }
    }

    // Candidate restaurant names, since the game names the restaurant only from the run
    // dish's own StartingNameSet.
    public override List<string> StartingNameSet {
      get => new List<string>
      {
                "Chez Castor",
                "Queue de Castor",
                "The Dam Good Dough",
                "Sugar Shack",
                "La Cabane a Sucre",
                "Toque and Tail",
                "The Flat Tail",
                "Pate Plate",
                "Castor Frit",
                "Fried and True",
                "The Rideau Roll",
                "Double Double Dough",
                "Le Canot Sucre",
                "Timber and Sugar",
                "Sorry About the Wait",
                "Beaver Please",
                "The Sweet Portage",
                "Eh! Pastry",
            };
      protected set { }
    }

    public override List<Dish.MenuItem> ResultingMenuItems {
      get => new List<Dish.MenuItem>
      {
                new Dish.MenuItem
                {
                    Item = Gdo.Own<Item>(BeaverTailClassicPlatedItem.NameId),
                    Phase = MenuPhase.Main,
                    Weight = 1f,
                },
            };
      protected set { }
    }

    // Drives which appliances the kitchen hands you.
    public static HashSet<Process> BaseProcesses() => new HashSet<Process>
    {
            Gdo.Process(ProcessReferences.Knead),
            Gdo.FryProcess(),
        };

    public override HashSet<Process> RequiredProcesses {
      get => BaseProcesses();
      protected set { }
    }

    // A provider appears for each of these, shared so the recipe cards can repeat them.
    public static HashSet<Item> BaseIngredients() => new HashSet<Item>
    {
            Gdo.Item(ItemReferences.Flour),
            Gdo.Item(ItemReferences.Milk),
            Gdo.Lib(Gdo.LibKeys.Cinnamon),
            Gdo.Item(ItemReferences.Sugar),
            Gdo.Item(ItemReferences.Plate),
        };

    public override HashSet<Item> MinimumIngredients {
      get => BaseIngredients();
      protected set { }
    }
  }
}
