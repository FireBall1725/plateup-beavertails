// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2026 FireBall1725

using System;
using System.Collections.Generic;
using Kitchen;
using KitchenData;
using KitchenMods;
using UnityEngine;

namespace BeaverTails {
  // GenericSystemBase, or a joining client never runs it and its recipe board comes up blank.
  public class RegisterRecipeTextSystem : GenericSystemBase, IModSystem {
    private static readonly (string DishNameId, string Text)[] Recipes =
    {
            (BeaverTailsDish.NameId,
                "Combine flour and milk into dough. Knead it flat and deep fry it. "
                + "Dust with cinnamon and sugar in either order, then plate."),

            (KillaloeSunriseCard.NameId,
                "Make a Classic beaver tail first, then add a slice of lemon. "
                + "Cinnamon and sugar go on before the lemon."),

            (HazelAmourCard.NameId,
                "Coat a fried beaver tail with hazelnut spread from a jar, then dust with "
                + "icing sugar and plate. Spread goes on first. One jar coats four tails."),

            (PistachOhCard.NameId,
                "Pistachio spread onto a fried tail, then honey poured over it, then a "
                + "scatter of chopped pistachios. One jar coats four tails; the honey "
                + "bottle and the crumb bin never run out."),

            (StrawberryCheesecakeCard.NameId,
                "Cheesecake spread onto a fried tail, then strawberry syrup poured over "
                + "it, then crushed crackers. One jar of spread coats four tails; the syrup "
                + "bottle and the cracker bin never run out."),

            (CocoVanilCard.NameId,
                "Vanilla icing onto a fried tail, then crushed cookies over it, then a "
                + "chocolate drizzle. One jar coats four tails; the cookie bin and the "
                + "syrup bottle never run out."),

            (BrwownieCard.NameId,
                "Brownies are the base game's recipe. Flour, sugar and a cracked egg into a "
                + "mixing bowl, then knead it. Melt chocolate on a hob. Bowl, melted chocolate "
                + "and a brownie tray together make a raw tray; bake it in the oven and it "
                + "breaks into six. Hazelnut spread onto a fried tail, then a piece of "
                + "brownie, then white chocolate chunks."),

            (PoutineCard.NameId,
                "Butter and flour in a pot make a roux; cook it. Add chopped meat to the cooked "
                + "roux and cook again for gravy, four servings. Milk and vinegar in a pot make "
                + "cheese curds, four servings. Chop a potato and fry it, then pour gravy over "
                + "the chips FIRST and add the curds after."),

            (DoubleCheesePoutineCard.NameId,
                "Build a poutine, then put a portion of cheese curds through the fryer "
                + "and add those on top. Fried curds burn if you leave them in."),

            (FriesCard.NameId,
                "Chop a potato and fry the pieces. Customers may order fries alongside "
                + "their beaver tail; put them on the same plate."),

            (BananaramaCard.NameId,
                "Peel a banana, chop it, then coat a fried beaver tail with hazelnut spread "
                + "and top it with the chopped banana. Spread goes on before the banana."),

            (MapleSyrupCard.NameId,
                "Customers ask for maple syrup while they are eating, after the tail has "
                + "been served. Take a bottle from the shelf and bring it to the table. One "
                + "bottle lasts all day and cannot be thrown away."),

            (MehpleCard.NameId,
                "Whip butter with maple syrup for maple butter. Add syrup to a pot of sugar "
                + "and cook it on the hob for maple sugar, eight servings a pot. Butter onto "
                + "a fried tail, then the sugar, then plate."),

            (AvalancheCard.NameId,
                "Cheesecake spread onto a fried tail, then Skor bits, then caramel. Sugar in a "
                + "pot on the hob makes the caramel, eight drizzles a pot. One jar of spread "
                + "coats four tails."),

            (TripleTripCard.NameId,
                "Hazelnut spread onto a fried tail, then peanut butter over it, then a "
                + "scatter of Reese's Pieces. Two jar racks, four servings a jar; the candy "
                + "bin never runs out."),

            (ApplePieCard.NameId,
                "Cook a pot with sugar and chopped apple to make filling. Cook another pot with "
                + "just sugar to make caramel. Cover a beaver tail with cinnamon and sugar (in "
                + "either order), then with filling, then with caramel. Plate and serve. One pot "
                + "gives four servings of filling or eight drizzles of caramel."),
        };

    private bool done;

    protected override void OnUpdate() {
      if ( done ) {
        return;
      }

      var recipes = GameData.Main?.GlobalLocalisation?.Recipes;
      if ( recipes?.Info == null ) {
        return;
      }

      // wait until every dish has registered, so a partial write is not latched
      var dishes = new List<(Dish Dish, string Text)>();
      foreach ( var (nameId, text) in Recipes ) {
        var dish = Gdo.Own<Dish>( nameId );
        if ( dish == null ) {
          return;
        }

        dishes.Add((dish, text));
      }

      done = true;

      // The board reads RecipeLocalisation.Text, so writing only into Info is too late to matter.
      if ( recipes.Text == null ) {
        recipes.Text = new Dictionary<Dish, string>();
      }

      var written = 0;

      foreach ( var (dish, text) in dishes ) {
        recipes.Text[dish] = text;

        foreach ( Locale locale in Enum.GetValues( typeof( Locale ))) {
          if ( !recipes.Info.Has( locale )) {
            continue;
          }

          var info = recipes.Info.Get( locale );

          // The check KitchenLib omits, using Unity's null operator so a destroyed native object is caught too.
          if ( info == null || info.Text == null ) {
            continue;
          }

          if ( !info.Text.ContainsKey( dish )) {
            info.Text.Add( dish, text );
            written++;
          }
        }
      }

      // Play state goes in the line because a client that never prints this is one whose systems are not running.
      Debug.Log( $"[BeaverTails] recipe text registered for {dishes.Count} dishes, "
                + $"{written} locale entries, as {Session.NetworkedPlayState}" );
    }
  }
}
