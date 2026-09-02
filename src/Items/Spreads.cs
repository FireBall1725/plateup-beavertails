// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2026 FireBall1725

using System.Collections.Generic;
using Kitchen;
using KitchenData;
using KitchenLib.Customs;
using KitchenLib.References;
using UnityEngine;

namespace BeaverTails {
  // A jar must not have any Processes, because a process removes the split interaction.
  public abstract class SpreadJarItem : CustomItem {
    protected abstract string DisplayName { get; }

    protected abstract Color JarColour { get; }

    protected abstract string PortionNameId { get; }

    private GameObject prefab;

    public override GameObject Prefab {
      get => prefab ?? ( prefab = BuildJar());
      protected set { }
    }

    private GameObject BuildJar() {
      var jar = Gdo.CloneFromBundle( "SpreadJar", DisplayName, JarColour );

      // No contents disc on a jar: the mesh is one solid piece in the spread's colour, so a
      // disc on the lid was the same brown on the same brown.
      Gdo.AddLevelBar( jar, JarColour );
      return jar;
    }

    public override Item SplitSubItem {
      get => Gdo.Own<Item>( PortionNameId );
      protected set { }
    }

    // Four uses then gone; an empty SplitDepletedItems destroys the container.
    public override int SplitCount {
      get => 4;
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

  // One portion, poured out of a jar and applied to a fried tail.
  public abstract class SpreadPortionItem : CustomItem {
    protected abstract string DisplayName { get; }

    protected abstract Color SpreadColour { get; }

    private GameObject prefab;

    // A ramekin with a swirled dollop, so the five spreads differ in shape and not only in colour.
    public override GameObject Prefab {
      get => prefab ?? ( prefab = Gdo.Portion(
          DisplayName, "SpreadRamekin", "SpreadDollop", SpreadColour, ColourBlindTag ));
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

  // A rack of jars; the scarcity is inside the jar, not the rack.
  public abstract class SpreadJarStackAppliance : CustomAppliance {
    protected abstract string JarNameId { get; }

    protected abstract string DisplayName { get; }

    protected abstract string Description { get; }

    protected abstract Color RackColour { get; }

    // The shelf is wood on both racks; only the jars carry the spread colour.
    private static Color Wood => new Color( 0.55f, 0.38f, 0.22f );

    private GameObject prefab;

    public override GameObject Prefab {
      get => prefab ?? ( prefab = BuildPrefab( DisplayName ));
      protected set { }
    }

    private GameObject heldPrefab;

    public override GameObject HeldAppliancePrefab {
      get => heldPrefab ?? ( heldPrefab = BuildPrefab( DisplayName + " (Held)" ));
      protected set { }
    }

    // A shelf with three jars standing in it, rather than a rack of pots tinted brown.
    private GameObject BuildPrefab( string name ) {
      var rack = Gdo.CloneAppliancePrefab(
          ApplianceReferences.PotStack, name, Color.white );

      var skin = Gdo.ReskinAppliance( rack, "JarRack", Wood );
      if ( skin != null ) {
        // No decorative jars: the pot stack's own LimitedItemSourceView spawns the real
        // ones, and adding ours produced two sets floating one above the other.
        var slots = new[]
        {
                    new Vector3(-0.28f, 0.07f, 0f),
                    new Vector3(0f, 0.07f, 0f),
                    new Vector3(0.28f, 0.07f, 0f),
                };

        Gdo.PlaceProviderSlots( rack, skin, slots, 1.3f );
      }

      return rack;
    }

    public override List<IApplianceProperty> Properties {
      get {
        var jar = Gdo.Own<Item>( JarNameId );
        if ( jar == null ) {
          return new List<IApplianceProperty>();
        }

        // Maximum 0 means infinite.
        return new List<IApplianceProperty>
        {
                    Gdo.Provider(jar),
                };
      }
      protected set { }
    }

    // buyable, so a busier kitchen can run a second rack
    public override bool IsPurchasable {
      get => true;
      protected set { }
    }

    public override ShoppingTags ShoppingTags {
      get => ShoppingTags.Cooking;
      protected set { }
    }

    private List<(Locale, ApplianceInfo)> cachedInfo;

    public override List<(Locale, ApplianceInfo)> InfoList {
      get => cachedInfo ?? ( cachedInfo = new List<(Locale, ApplianceInfo)>
      {
                (Locale.English, Gdo.ApplianceInfo(DisplayName, Description)),
            } );
      protected set { }
    }

    // Items register first so this can resolve ProvidedItem, then it hands the item its
    // provider back, which is what makes a dispenser appear in MinimumIngredients.
    public override void OnRegister( Appliance gameDataObject ) {
      base.OnRegister( gameDataObject );
      Gdo.PointItemAtProvider( JarNameId, gameDataObject );

      // Racks are purchasable, so gate on the jar or they go on sale whether or not the
      // spread is unlocked.
      Gdo.RequireForShop( gameDataObject, Gdo.Own<Item>( JarNameId ));
    }
  }

  public class HazelnutJarItem : SpreadJarItem {
    public const string NameId = "hazelnut_jar";

    public static Color HazelnutColour => new Color( 0.29f, 0.16f, 0.09f );

    public override string UniqueNameID => NameId;

    protected override string DisplayName => "Chocolate Hazelnut Spread";

    protected override Color JarColour => HazelnutColour;

    protected override string PortionNameId => HazelnutSpreadItem.NameId;

    public override string ColourBlindTag {
      get => "HJ";
      protected set { }
    }
  }

  public class HazelnutSpreadItem : SpreadPortionItem {
    public const string NameId = "hazelnut_spread";

    public override string UniqueNameID => NameId;

    protected override string DisplayName => "Hazelnut Spread";

    protected override Color SpreadColour => HazelnutJarItem.HazelnutColour;

    public override string ColourBlindTag {
      get => "HS";
      protected set { }
    }
  }

  public class JarStackAppliance : SpreadJarStackAppliance {
    public const string NameId = "jar_stack";

    public override string UniqueNameID => NameId;

    protected override string JarNameId => HazelnutJarItem.NameId;

    protected override string DisplayName => "Hazelnut Spread Jars";

    protected override string Description =>
        "Jars of chocolate hazelnut spread, four servings each.";

    protected override Color RackColour => HazelnutJarItem.HazelnutColour;
  }

  public class PeanutJarItem : SpreadJarItem {
    public const string NameId = "peanut_jar";

    public static Color PeanutColour => new Color( 0.72f, 0.51f, 0.24f );

    public override string UniqueNameID => NameId;

    protected override string DisplayName => "Peanut Butter";

    protected override Color JarColour => PeanutColour;

    protected override string PortionNameId => PeanutButterItem.NameId;

    public override string ColourBlindTag {
      get => "PJ";
      protected set { }
    }
  }

  public class PeanutButterItem : SpreadPortionItem {
    public const string NameId = "peanut_butter";

    public override string UniqueNameID => NameId;

    protected override string DisplayName => "Peanut Butter";

    protected override Color SpreadColour => PeanutJarItem.PeanutColour;

    public override string ColourBlindTag {
      get => "PB";
      protected set { }
    }
  }

  public class PeanutJarStackAppliance : SpreadJarStackAppliance {
    public const string NameId = "peanut_jar_stack";

    public override string UniqueNameID => NameId;

    protected override string JarNameId => PeanutJarItem.NameId;

    protected override string DisplayName => "Peanut Butter Jars";

    protected override string Description =>
        "Jars of peanut butter, four servings each.";

    protected override Color RackColour => PeanutJarItem.PeanutColour;
  }

  public class CheesecakeJarItem : SpreadJarItem {
    public const string NameId = "cheesecake_jar";

    public static Color CheesecakeColour => new Color( 0.93f, 0.83f, 0.66f );

    public override string UniqueNameID => NameId;

    protected override string DisplayName => "Cheesecake Spread";

    protected override Color JarColour => CheesecakeColour;

    protected override string PortionNameId => CheesecakeSpreadItem.NameId;

    public override string ColourBlindTag {
      get => "CJ";
      protected set { }
    }
  }

  public class CheesecakeSpreadItem : SpreadPortionItem {
    public const string NameId = "cheesecake_spread";

    public override string UniqueNameID => NameId;

    protected override string DisplayName => "Cheesecake Spread";

    protected override Color SpreadColour => CheesecakeJarItem.CheesecakeColour;

    public override string ColourBlindTag {
      get => "Ck";
      protected set { }
    }
  }

  public class CheesecakeJarStackAppliance : SpreadJarStackAppliance {
    public const string NameId = "cheesecake_jar_stack";

    public override string UniqueNameID => NameId;

    protected override string JarNameId => CheesecakeJarItem.NameId;

    protected override string DisplayName => "Cheesecake Spread Jars";

    protected override string Description =>
        "Jars of cheesecake spread, four servings each.";

    protected override Color RackColour => CheesecakeJarItem.CheesecakeColour;
  }

  public class PistachioJarItem : SpreadJarItem {
    public const string NameId = "pistachio_jar";

    public static Color PistachioColour => new Color( 0.62f, 0.71f, 0.42f );

    public override string UniqueNameID => NameId;

    protected override string DisplayName => "Pistachio Spread";

    protected override Color JarColour => PistachioColour;

    protected override string PortionNameId => PistachioSpreadItem.NameId;

    public override string ColourBlindTag {
      get => "Pt";
      protected set { }
    }
  }

  public class PistachioSpreadItem : SpreadPortionItem {
    public const string NameId = "pistachio_spread";

    public override string UniqueNameID => NameId;

    protected override string DisplayName => "Pistachio Spread";

    protected override Color SpreadColour => PistachioJarItem.PistachioColour;

    public override string ColourBlindTag {
      get => "Pp";
      protected set { }
    }
  }

  public class PistachioJarStackAppliance : SpreadJarStackAppliance {
    public const string NameId = "pistachio_jar_stack";

    public override string UniqueNameID => NameId;

    protected override string JarNameId => PistachioJarItem.NameId;

    protected override string DisplayName => "Pistachio Spread Jars";

    protected override string Description =>
        "Jars of pistachio spread, four servings each.";

    protected override Color RackColour => PistachioJarItem.PistachioColour;
  }

  public class VanillaIcingJarItem : SpreadJarItem {
    public const string NameId = "vanilla_icing_jar";

    public static Color IcingColour => new Color( 0.97f, 0.95f, 0.88f );

    public override string UniqueNameID => NameId;

    protected override string DisplayName => "Vanilla Icing";

    protected override Color JarColour => IcingColour;

    protected override string PortionNameId => VanillaIcingItem.NameId;

    public override string ColourBlindTag {
      get => "Vj";
      protected set { }
    }
  }

  public class VanillaIcingItem : SpreadPortionItem {
    public const string NameId = "vanilla_icing";

    public override string UniqueNameID => NameId;

    protected override string DisplayName => "Vanilla Icing";

    protected override Color SpreadColour => VanillaIcingJarItem.IcingColour;

    public override string ColourBlindTag {
      get => "Vi";
      protected set { }
    }
  }

  public class VanillaIcingJarStackAppliance : SpreadJarStackAppliance {
    public const string NameId = "vanilla_icing_jar_stack";

    public override string UniqueNameID => NameId;

    protected override string JarNameId => VanillaIcingJarItem.NameId;

    protected override string DisplayName => "Vanilla Icing Jars";

    protected override string Description =>
        "Jars of vanilla icing, four servings each.";

    protected override Color RackColour => VanillaIcingJarItem.IcingColour;
  }
}
