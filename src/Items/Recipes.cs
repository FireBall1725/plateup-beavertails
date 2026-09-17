// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2026 FireBall1725

using System.Collections.Generic;
using Kitchen;
using KitchenData;
using KitchenLib.Customs;
using KitchenLib.References;
using UnityEngine;

namespace BeaverTails {
  // The named products, each a fixed recipe rather than a tail with optional extras.

  // A fried tail wearing one of the two dustings.
  //
  // Its own item rather than a half-built Classic, because a Partial ItemGroup survives being
  // plated as a complete one: AttemptComponentMerge reads the PLATE's satisfaction and not the
  // tail's, so a tail with cinnamon alone used to serve as a finished Cinnamon Sugar Tail and
  // wore the finished model while it did.
  public abstract class BeaverTailDustedItem : CustomItemGroup {
    protected abstract string TailName { get; }

    protected abstract (string Asset, string Child, Color Colour) Dusting { get; }

    // A property rather than a field, because IngredientLib fills its dictionaries during its own Convert.
    protected abstract Item DustingItem { get; }

    protected abstract string DustingTag { get; }

    private GameObject prefab;

    public override GameObject Prefab {
      get => prefab ?? ( prefab = BeaverTailClassicItem.BuildDusted( TailName, Dusting ));
      protected set { }
    }

    // One mandatory set of two, the shape every other stage in the mod uses: the single merge
    // completes it, so this item is never Partial.
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
                        Gdo.Own<Item>(BeaverTailCookedItem.NameId),
                        DustingItem,
                    },
                },
            };
      protected set { }
    }

    public override ItemValue ItemValue {
      get => ItemValue.Small;
      protected set { }
    }

    public override List<ItemGroupView.ColourBlindLabel> Labels {
      get => new List<ItemGroupView.ColourBlindLabel>
      {
                new ItemGroupView.ColourBlindLabel { Item = DustingItem, Text = DustingTag },
            };
      protected set { }
    }

    public override string ColourBlindTag {
      get => DustingTag;
      protected set { }
    }
  }

  public class BeaverTailCinnamonItem : BeaverTailDustedItem {
    public const string NameId = "beavertail_cinnamon";

    public override string UniqueNameID => NameId;

    protected override string TailName => "Beaver Tail - Cinnamon";

    protected override (string Asset, string Child, Color Colour) Dusting =>
        BeaverTailClassicItem.CinnamonDusting;

    protected override Item DustingItem => Gdo.Lib( Gdo.LibKeys.Cinnamon );

    protected override string DustingTag => "C";
  }

  public class BeaverTailSugarItem : BeaverTailDustedItem {
    public const string NameId = "beavertail_sugar";

    public override string UniqueNameID => NameId;

    protected override string TailName => "Beaver Tail - Sugar";

    protected override (string Asset, string Child, Color Colour) Dusting =>
        BeaverTailClassicItem.SugarDusting;

    protected override Item DustingItem => Gdo.Item( ItemReferences.Sugar );

    protected override string DustingTag => "S";
  }

  // The finished dusted tail: a once-dusted tail plus the other dusting, in either order.
  public class BeaverTailClassicItem : CustomItemGroup {
    public const string NameId = "beavertail_classic";

    // Card art only; the item itself is a cooked tail with the dustings as children.
    public static Color Colour => new Color( 0.80f, 0.60f, 0.36f );

    public override string UniqueNameID => NameId;

    private GameObject prefab;

    // Cinnamon and sugar are separate children so the single-dusting stages can take one each.
    public override GameObject Prefab {
      get => prefab ?? ( prefab = BuildClassicTail( "Beaver Tail - Classic" ));
      protected set { }
    }

    private const string CinnamonChild = "Cinnamon Dusting";
    private const string SugarChild = "Sugar Dusting";

    internal static (string Asset, string Child, Color Colour) CinnamonDusting =>
        ("CinnamonDusting", CinnamonChild, Cinnamon);

    internal static (string Asset, string Child, Color Colour) SugarDusting =>
        ("SugarDusting", SugarChild, Sugar);

    // A fried tail wearing both dustings, shared with Killaloe.
    internal static GameObject BuildClassicTail( string name ) =>
        BuildDusted( name, CinnamonDusting, SugarDusting );

    internal static GameObject BuildDusted(
        string name, params (string Asset, string Child, Color Colour)[] dustings ) {
      var tail = Gdo.CloneFromBundle( "BeaverTail", name, CookedColour );
      if ( tail == null ) {
        return null;
      }

      foreach ( var (asset, child, colour) in dustings ) {
        var dusting = Gdo.CloneFromBundle( asset, child, colour );
        if ( dusting == null ) {
          continue;
        }

        dusting.transform.SetParent( tail.transform, false );
        dusting.transform.localPosition = Vector3.zero;

        // Left on: ItemGroupView only hides components on its first in-game update.
        dusting.SetActive( true );
      }

      return tail;
    }

    // No component mappings, because a Classic always carries both dustings and the components
    // it reports are the once-dusted tail and a dusting, neither of which names a child here.
    // A mapping would hide whichever dusting went on first.
    public override void OnRegister( ItemGroup gameDataObject ) {
      base.OnRegister( gameDataObject );

      var tail = gameDataObject?.Prefab;
      if ( tail == null ) {
        return;
      }

      var view = tail.GetComponent<ItemGroupView>() ?? tail.AddComponent<ItemGroupView>();
      view.ComponentGroups = new List<ItemGroupView.ComponentGroup>();

      foreach ( var child in new[] { CinnamonChild, SugarChild } ) {
        var found = tail.transform.Find( child );
        if ( found == null ) {
          Debug.LogWarning( $"[BeaverTails] Classic: no '{child}' on the prefab" );
          continue;
        }

        found.gameObject.SetActive( true );
      }
    }

    internal static Color CookedColour => new Color( 0.72f, 0.47f, 0.22f );

    internal static Color Cinnamon => new Color( 0.55f, 0.33f, 0.16f );

    internal static Color Sugar => new Color( 0.96f, 0.94f, 0.90f );

    // Two mandatory sets of one: the once-dusted tail, and the dusting it still needs. Both are
    // satisfied by the single merge that finishes the recipe, so a Classic is never Partial,
    // and a tail carrying only one dusting is a different item that no plate will accept.
    public override List<ItemGroup.ItemSet> Sets {
      get => new List<ItemGroup.ItemSet>
      {
                new ItemGroup.ItemSet
                {
                    Min = 1,
                    Max = 1,
                    IsMandatory = true,
                    Items = new List<Item>
                    {
                        Gdo.Own<Item>(BeaverTailCinnamonItem.NameId),
                        Gdo.Own<Item>(BeaverTailSugarItem.NameId),
                    },
                },
                new ItemGroup.ItemSet
                {
                    Min = 1,
                    Max = 1,
                    IsMandatory = true,
                    Items = new List<Item>
                    {
                        Gdo.Lib(Gdo.LibKeys.Cinnamon),
                        Gdo.Item(ItemReferences.Sugar),
                    },
                },
            };
      protected set { }
    }

    public override ItemValue ItemValue {
      get => ItemValue.Small;
      protected set { }
    }

    // An ItemGroup's label comes from the view's ComponentLabels, not from ColourBlindTag. The
    // view walks this list in order, not the components, so cinnamon-then-sugar and
    // sugar-then-cinnamon both read CS.
    public override List<ItemGroupView.ColourBlindLabel> Labels {
      get => new List<ItemGroupView.ColourBlindLabel>
      {
                new ItemGroupView.ColourBlindLabel { Item = Gdo.Own<Item>(BeaverTailCinnamonItem.NameId), Text = "C" },
                new ItemGroupView.ColourBlindLabel { Item = Gdo.Lib(Gdo.LibKeys.Cinnamon), Text = "C" },
                new ItemGroupView.ColourBlindLabel { Item = Gdo.Own<Item>(BeaverTailSugarItem.NameId), Text = "S" },
                new ItemGroupView.ColourBlindLabel { Item = Gdo.Item(ItemReferences.Sugar), Text = "S" },
            };
      protected set { }
    }

    public override string ColourBlindTag {
      get => "CS";
      protected set { }
    }
  }

  // The Classic with a slice of lemon, built on the Classic.
  public class BeaverTailKillaloeItem : CustomItemGroup {
    public const string NameId = "beavertail_killaloe";

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
                    IsMandatory = true,
                    Items = new List<Item>
                    {
                        Gdo.Own<Item>(BeaverTailClassicItem.NameId),
                        Gdo.Item(ItemReferences.LemonSliced),
                    },
                },
            };
      protected set { }
    }

    public override ItemValue ItemValue {
      get => ItemValue.Small;
      protected set { }
    }

    public override List<ItemGroupView.ColourBlindLabel> Labels {
      get => new List<ItemGroupView.ColourBlindLabel>
      {
                new ItemGroupView.ColourBlindLabel { Item = Gdo.Item(ItemReferences.LemonSliced), Text = "KS" },
            };
      protected set { }
    }

    public override string ColourBlindTag {
      get => "KS";
      protected set { }
    }

    private static GameObject BuildPrefab() {
      // Killaloe is the Classic with a lemon slice, so it is built the same way.
      var tail = BeaverTailClassicItem.BuildClassicTail( "Beaver Tail - Killaloe Sunrise" );
      if ( tail == null ) {
        return null;
      }

      var lemon = Gdo.Item( ItemReferences.LemonSliced )?.Prefab;
      if ( lemon != null ) {
        var slice = Gdo.Unlabel( Object.Instantiate( lemon, tail.transform ));
        slice.name = "Lemon Slice";
        slice.transform.localPosition = new Vector3( 0.18f, 0.08f, 0f );
        slice.transform.localScale = Vector3.one * 0.6f;
      }

      return tail;
    }
  }

  public class BeaverTailHazelnutCoatedItem : CustomItemGroup {
    public const string NameId = "beavertail_hazelnut_coated";

    public override string UniqueNameID => NameId;

    private GameObject prefab;

    public override GameObject Prefab {
      get => prefab ?? ( prefab = Gdo.CloneFromBundle(
          "BeaverTail", "Beaver Tail - Hazelnut Coated", HazelnutJarItem.HazelnutColour ));
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
                        Gdo.Own<Item>(BeaverTailCookedItem.NameId),
                        Gdo.Own<Item>(HazelnutSpreadItem.NameId),
                    },
                },
            };
      protected set { }
    }

    public override ItemValue ItemValue {
      get => ItemValue.Small;
      protected set { }
    }

    public override List<ItemGroupView.ColourBlindLabel> Labels {
      get => new List<ItemGroupView.ColourBlindLabel>
      {
                new ItemGroupView.ColourBlindLabel { Item = Gdo.Own<Item>(HazelnutSpreadItem.NameId), Text = "HZ" },
            };
      protected set { }
    }

    public override string ColourBlindTag {
      get => "HZ";
      protected set { }
    }
  }

  // Pistach-OH, stage one: a fried tail coated in pistachio spread.
  public class BeaverTailPistachioCoatedItem : CustomItemGroup {
    public const string NameId = "beavertail_pistachio_coated";

    public override string UniqueNameID => NameId;

    private GameObject prefab;

    public override GameObject Prefab {
      get => prefab ?? ( prefab = Gdo.CloneFromBundle(
          "BeaverTail", "Beaver Tail - Pistachio Coated", PistachioJarItem.PistachioColour ));
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
                        Gdo.Own<Item>(BeaverTailCookedItem.NameId),
                        Gdo.Own<Item>(PistachioSpreadItem.NameId),
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
                    Item = Gdo.Own<Item>(PistachioSpreadItem.NameId),
                    Text = "Pk",
                },
            };
      protected set { }
    }

    public override ItemValue ItemValue {
      get => ItemValue.Small;
      protected set { }
    }

    public override string ColourBlindTag {
      get => "Pk";
      protected set { }
    }
  }

  // Honey uses IngredientLib's poured serving rather than its bottle.
  public class BeaverTailPistachioHoneyItem : CustomItemGroup {
    public const string NameId = "beavertail_pistachio_honey";

    public static Color Honeyed => new Color( 0.72f, 0.66f, 0.31f );

    public override string UniqueNameID => NameId;

    private GameObject prefab;

    public override GameObject Prefab {
      get => prefab ?? ( prefab = Gdo.CloneFromBundle(
          "BeaverTail", "Beaver Tail - Pistachio and Honey", Honeyed ));
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
                        Gdo.Own<Item>(BeaverTailPistachioCoatedItem.NameId),
                        Gdo.LibSplit(Gdo.LibKeys.Honey),
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
                    Item = Gdo.LibSplit(Gdo.LibKeys.Honey),
                    Text = "Ho",
                },
            };
      protected set { }
    }

    public override ItemValue ItemValue {
      get => ItemValue.Small;
      protected set { }
    }

    public override string ColourBlindTag {
      get => "Ho";
      protected set { }
    }
  }

  // Pistach-OH: finished with chopped pistachios.
  public class BeaverTailPistachOhItem : CustomItemGroup {
    public const string NameId = "beavertail_pistachoh";

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
                    IsMandatory = true,
                    Items = new List<Item>
                    {
                        Gdo.Own<Item>(BeaverTailPistachioHoneyItem.NameId),
                        Gdo.Own<Item>(PistachioCrumbsItem.NameId),
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
                    Item = Gdo.Own<Item>(PistachioCrumbsItem.NameId),
                    Text = "PO",
                },
            };
      protected set { }
    }

    public override ItemValue ItemValue {
      get => ItemValue.Small;
      protected set { }
    }

    public override string ColourBlindTag {
      get => "PO";
      protected set { }
    }

    // SkorScatter is authored in the tail's own coordinate space, so it drops straight on.
    private static GameObject BuildPrefab() {
      var tail = Gdo.CloneFromBundle(
          "BeaverTail", "Beaver Tail - Pistach-OH", BeaverTailPistachioHoneyItem.Honeyed );
      if ( tail == null ) {
        return null;
      }

      var crumbs = Gdo.CloneFromBundle(
          "PistachioCrumbs", "Pistachio Crumbs", PistachioCrumbsItem.Green );
      if ( crumbs != null ) {
        crumbs.transform.SetParent( tail.transform, false );
        crumbs.transform.localPosition = Vector3.zero;
      }

      return tail;
    }
  }

  // Uses the base game's sundae strawberry syrup.
  public class BeaverTailCheesecakeJamItem : CustomItemGroup {
    public const string NameId = "beavertail_cheesecake_jam";

    public static Color Strawberry => new Color( 0.78f, 0.24f, 0.29f );

    public override string UniqueNameID => NameId;

    private GameObject prefab;

    public override GameObject Prefab {
      get => prefab ?? ( prefab = Gdo.CloneFromBundle(
          "BeaverTail", "Beaver Tail - Cheesecake and Strawberry", Strawberry ));
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
                        Gdo.Own<Item>(BeaverTailCheesecakeCoatedItem.NameId),
                        Gdo.Item(ItemReferences.SundaeSyrupStrawberryServing),
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
                    Item = Gdo.Item(ItemReferences.SundaeSyrupStrawberryServing),
                    Text = "Sj",
                },
            };
      protected set { }
    }

    public override ItemValue ItemValue {
      get => ItemValue.Small;
      protected set { }
    }

    public override string ColourBlindTag {
      get => "Sj";
      protected set { }
    }
  }

  // Finished with crushed crackers, the graham-cracker base of a cheesecake scattered on top.
  public class BeaverTailStrawberryCheesecakeItem : CustomItemGroup {
    public const string NameId = "beavertail_strawberry_cheesecake";

    public static Color Cracker => new Color( 0.79f, 0.64f, 0.41f );

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
                    IsMandatory = true,
                    Items = new List<Item>
                    {
                        Gdo.Own<Item>(BeaverTailCheesecakeJamItem.NameId),
                        Gdo.Lib(Gdo.LibKeys.Crackers),
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
                    Item = Gdo.Lib(Gdo.LibKeys.Crackers),
                    Text = "SC",
                },
            };
      protected set { }
    }

    public override ItemValue ItemValue {
      get => ItemValue.Small;
      protected set { }
    }

    public override string ColourBlindTag {
      get => "SC";
      protected set { }
    }

    private static GameObject BuildPrefab() {
      var tail = Gdo.CloneFromBundle(
          "BeaverTail", "Beaver Tail - Strawberry Cheesecake",
          BeaverTailCheesecakeJamItem.Strawberry );
      if ( tail == null ) {
        return null;
      }

      var crumbs = Gdo.CloneFromBundle( "CrackerCrumbs", "Cracker Crumbs", Cracker );
      if ( crumbs != null ) {
        crumbs.transform.SetParent( tail.transform, false );
        crumbs.transform.localPosition = Vector3.zero;
      }

      return tail;
    }
  }

  // Coco Vanil, stage one: a fried tail coated in vanilla icing.
  public class BeaverTailVanillaCoatedItem : CustomItemGroup {
    public const string NameId = "beavertail_vanilla_coated";

    public override string UniqueNameID => NameId;

    private GameObject prefab;

    public override GameObject Prefab {
      get => prefab ?? ( prefab = Gdo.CloneFromBundle(
          "BeaverTail", "Beaver Tail - Vanilla Coated", VanillaIcingJarItem.IcingColour ));
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
                        Gdo.Own<Item>(BeaverTailCookedItem.NameId),
                        Gdo.Own<Item>(VanillaIcingItem.NameId),
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
                    Item = Gdo.Own<Item>(VanillaIcingItem.NameId),
                    Text = "Vc",
                },
            };
      protected set { }
    }

    public override ItemValue ItemValue {
      get => ItemValue.Small;
      protected set { }
    }

    public override string ColourBlindTag {
      get => "Vc";
      protected set { }
    }
  }

  // Stage two: crushed cookies over the icing, dark crumbs on a white coat.
  public class BeaverTailVanillaOreoItem : CustomItemGroup {
    public const string NameId = "beavertail_vanilla_oreo";

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
                    IsMandatory = true,
                    Items = new List<Item>
                    {
                        Gdo.Own<Item>(BeaverTailVanillaCoatedItem.NameId),
                        Gdo.Own<Item>(CrushedOreoItem.NameId),
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
                    Item = Gdo.Own<Item>(CrushedOreoItem.NameId),
                    Text = "Vo",
                },
            };
      protected set { }
    }

    public override ItemValue ItemValue {
      get => ItemValue.Small;
      protected set { }
    }

    public override string ColourBlindTag {
      get => "Vo";
      protected set { }
    }

    private static GameObject BuildPrefab() {
      var tail = Gdo.CloneFromBundle(
          "BeaverTail", "Beaver Tail - Vanilla and Cookies",
          VanillaIcingJarItem.IcingColour );
      if ( tail == null ) {
        return null;
      }

      var crumbs = Gdo.CloneFromBundle( "CookieCrumbs", "Cookie Crumbs", CrushedOreoItem.Cookie );
      if ( crumbs != null ) {
        crumbs.transform.SetParent( tail.transform, false );
        crumbs.transform.localPosition = Vector3.zero;
      }

      return tail;
    }
  }

  // Uses the base game's sundae chocolate syrup.
  public class BeaverTailCocoVanilItem : CustomItemGroup {
    public const string NameId = "beavertail_coco_vanil";

    public static Color Sauce => new Color( 0.40f, 0.24f, 0.14f );

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
                    IsMandatory = true,
                    Items = new List<Item>
                    {
                        Gdo.Own<Item>(BeaverTailVanillaOreoItem.NameId),
                        Gdo.Item(ItemReferences.SundaeSyrupChocolateServing),
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
                    Item = Gdo.Item(ItemReferences.SundaeSyrupChocolateServing),
                    Text = "CV",
                },
            };
      protected set { }
    }

    public override ItemValue ItemValue {
      get => ItemValue.Small;
      protected set { }
    }

    public override string ColourBlindTag {
      get => "CV";
      protected set { }
    }

    private static GameObject BuildPrefab() {
      var tail = Gdo.CloneFromBundle(
          "BeaverTail", "Beaver Tail - Coco Vanil", VanillaIcingJarItem.IcingColour );
      if ( tail == null ) {
        return null;
      }

      var crumbs = Gdo.CloneFromBundle( "CookieCrumbs", "Cookie Crumbs", CrushedOreoItem.Cookie );
      if ( crumbs != null ) {
        crumbs.transform.SetParent( tail.transform, false );
        crumbs.transform.localPosition = Vector3.zero;
      }

      var drizzle = Gdo.CloneFromBundle( "DrizzleRibbon", "Chocolate Drizzle", Sauce );
      if ( drizzle != null ) {
        drizzle.transform.SetParent( tail.transform, false );
        drizzle.transform.localPosition = Vector3.zero;
      }

      return tail;
    }
  }

  // Reuses the hazelnut-coated tail, so four cards share one rack.
  public class BeaverTailBrownieItem : CustomItemGroup {
    // The vanilla Brownie carries CPreventItemMerge, which blocks the set below until it is cleared.
    public override void OnRegister( ItemGroup gameDataObject ) {
      base.OnRegister( gameDataObject );
    }

    public const string NameId = "beavertail_brownie";

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
                    IsMandatory = true,
                    Items = new List<Item>
                    {
                        Gdo.Own<Item>(BeaverTailHazelnutCoatedItem.NameId),
                        Gdo.Item(ItemReferences.Brownie),
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
                    Item = Gdo.Item(ItemReferences.Brownie),
                    Text = "Bn",
                },
            };
      protected set { }
    }

    public override ItemValue ItemValue {
      get => ItemValue.Small;
      protected set { }
    }

    public override string ColourBlindTag {
      get => "Bn";
      protected set { }
    }

    private static GameObject BuildPrefab() {
      var tail = Gdo.CloneFromBundle(
          "BeaverTail", "Beaver Tail - Brownie", HazelnutJarItem.HazelnutColour );
      if ( tail == null ) {
        return null;
      }

      var pieces = Gdo.CloneFromBundle(
          "BrownieChunks", "Brownie Pieces", BrownieArt.Chocolate );
      if ( pieces != null ) {
        pieces.transform.SetParent( tail.transform, false );
        pieces.transform.localPosition = Vector3.zero;
      }

      return tail;
    }
  }

  // BrWOWnie: finished with white chocolate chunks, pale against everything under them.
  public class BeaverTailBrwownieItem : CustomItemGroup {
    public const string NameId = "beavertail_brwownie";

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
                    IsMandatory = true,
                    Items = new List<Item>
                    {
                        Gdo.Own<Item>(BeaverTailBrownieItem.NameId),
                        Gdo.Own<Item>(WhiteChocolateChunksItem.NameId),
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
                    Item = Gdo.Own<Item>(WhiteChocolateChunksItem.NameId),
                    Text = "WW",
                },
            };
      protected set { }
    }

    public override ItemValue ItemValue {
      get => ItemValue.Small;
      protected set { }
    }

    public override string ColourBlindTag {
      get => "WW";
      protected set { }
    }

    private static GameObject BuildPrefab() {
      var tail = Gdo.CloneFromBundle(
          "BeaverTail", "Beaver Tail - BrWOWnie", HazelnutJarItem.HazelnutColour );
      if ( tail == null ) {
        return null;
      }

      var brownie = Gdo.CloneFromBundle(
          "BrownieChunks", "Brownie Pieces", BrownieArt.Chocolate );
      if ( brownie != null ) {
        brownie.transform.SetParent( tail.transform, false );
        brownie.transform.localPosition = Vector3.zero;
      }

      var white = Gdo.CloneFromBundle(
          "WhiteChocolateShards", "White Chocolate", WhiteChocolateChunksItem.Cream );
      if ( white != null ) {
        white.transform.SetParent( tail.transform, false );
        white.transform.localPosition = Vector3.zero;
      }

      return tail;
    }
  }

  // Hazel Amour: the coated tail dusted with icing sugar.
  public class BeaverTailHazelAmourItem : CustomItemGroup {
    public const string NameId = "beavertail_hazel_amour";

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
                    IsMandatory = true,
                    Items = new List<Item>
                    {
                        Gdo.Own<Item>(BeaverTailHazelnutCoatedItem.NameId),
                        Gdo.Item(ItemReferences.Sugar),
                    },
                },
            };
      protected set { }
    }

    public override ItemValue ItemValue {
      get => ItemValue.Small;
      protected set { }
    }

    public override List<ItemGroupView.ColourBlindLabel> Labels {
      get => new List<ItemGroupView.ColourBlindLabel>
      {
                new ItemGroupView.ColourBlindLabel { Item = Gdo.Item(ItemReferences.Sugar), Text = "HA" },
            };
      protected set { }
    }

    public override string ColourBlindTag {
      get => "HA";
      protected set { }
    }

    private static GameObject BuildPrefab() {
      var tail = Gdo.CloneFromBundle(
          "BeaverTail", "Beaver Tail - Hazel Amour", HazelnutJarItem.HazelnutColour );
      if ( tail == null ) {
        return null;
      }

      // SugarDusting is authored in the tail's own coordinate space, so it drops on at the origin.
      var dusting = Gdo.CloneFromBundle(
          "SugarDusting", "Icing Sugar", new Color( 0.96f, 0.94f, 0.90f ));
      if ( dusting != null ) {
        dusting.transform.SetParent( tail.transform, false );
        dusting.transform.localPosition = Vector3.zero;
      }

      return tail;
    }
  }

  // The same hazelnut-coated tail as Hazel Amour, finished with banana slices.
  public class BeaverTailBananaramaItem : CustomItemGroup {
    public const string NameId = "beavertail_bananarama";

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
                    IsMandatory = true,
                    Items = new List<Item>
                    {
                        Gdo.Own<Item>(BeaverTailHazelnutCoatedItem.NameId),
                        Gdo.Lib(Gdo.LibKeys.ChoppedBanana),
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
                    Item = Gdo.Lib(Gdo.LibKeys.ChoppedBanana),
                    Text = "BR",
                },
            };
      protected set { }
    }

    public override ItemValue ItemValue {
      get => ItemValue.Small;
      protected set { }
    }

    public override string ColourBlindTag {
      get => "BR";
      protected set { }
    }

    private static Color BananaFlesh => new Color( 0.96f, 0.91f, 0.68f );

    private static GameObject BuildPrefab() {
      var tail = Gdo.CloneFromBundle(
          "BeaverTail", "Beaver Tail - Bananarama", HazelnutJarItem.HazelnutColour );
      if ( tail == null ) {
        return null;
      }

      // The slices on the tail are our own mesh; the chopped banana in the recipe is IngredientLib's.
      var slices = Gdo.CloneFromBundle(
          "BananaSlices", "Banana Slices", BananaFlesh );
      if ( slices != null ) {
        slices.transform.SetParent( tail.transform, false );
        slices.transform.localPosition = Vector3.zero;
      }

      return tail;
    }
  }

  // Chopped apple over a fried tail.
  public class BeaverTailAppleItem : CustomItemGroup {
    public const string NameId = "beavertail_apple";

    public static Color Colour => new Color( 0.91f, 0.72f, 0.31f );

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
                    IsMandatory = true,
                    Items = new List<Item>
                    {
                        Gdo.Own<Item>(BeaverTailClassicItem.NameId),
                        Gdo.Own<Item>(ApplePieFillingItem.NameId),
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
                    Item = Gdo.Own<Item>(ApplePieFillingItem.NameId),
                    Text = "Ap",
                },
            };
      protected set { }
    }

    public override ItemValue ItemValue {
      get => ItemValue.Small;
      protected set { }
    }

    public override string ColourBlindTag {
      get => "Ap";
      protected set { }
    }

    // reuses the banana-slice scatter, retinted: apple pieces are rounds too
    private static GameObject BuildPrefab() {
      var tail = Gdo.CloneFromBundle( "BeaverTail", "Beaver Tail - Apple Filling", Colour );
      if ( tail == null ) {
        return null;
      }

      var pieces = Gdo.CloneFromBundle(
          "AppleChunks", "Apple Filling", new Color( 0.94f, 0.89f, 0.72f ));
      if ( pieces != null ) {
        pieces.transform.SetParent( tail.transform, false );
        pieces.transform.localPosition = Vector3.zero;
      }

      return tail;
    }
  }

  // Apple Pie: the Classic, then apple filling, finished with a caramel drizzle.
  public class BeaverTailApplePieItem : CustomItemGroup {
    public const string NameId = "beavertail_apple_pie";

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
                    IsMandatory = true,
                    Items = new List<Item>
                    {
                        Gdo.Own<Item>(BeaverTailAppleItem.NameId),
                        Gdo.Lib(Gdo.LibKeys.Caramel),
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
                    Item = Gdo.Lib(Gdo.LibKeys.Caramel),
                    Text = "AP",
                },
            };
      protected set { }
    }

    public override ItemValue ItemValue {
      get => ItemValue.Small;
      protected set { }
    }

    public override string ColourBlindTag {
      get => "AP";
      protected set { }
    }

    // apple pieces, then the caramel drizzle scattered over them
    private static GameObject BuildPrefab() {
      var tail = Gdo.CloneFromBundle(
          "BeaverTail", "Beaver Tail - Apple Pie", BeaverTailAppleItem.Colour );
      if ( tail == null ) {
        return null;
      }

      var pieces = Gdo.CloneFromBundle(
          "AppleChunks", "Apple Filling", new Color( 0.94f, 0.89f, 0.72f ));
      if ( pieces != null ) {
        pieces.transform.SetParent( tail.transform, false );
        pieces.transform.localPosition = Vector3.zero;
      }

      var drizzle = Gdo.CloneFromBundle(
          "DrizzleRibbon", "Caramel Drizzle", PotWithCaramelItem.Amber );
      if ( drizzle != null ) {
        drizzle.transform.SetParent( tail.transform, false );
        // DrizzleRibbon is authored clear of the topping layer, so it needs no lift.
        drizzle.transform.localPosition = Vector3.zero;
      }

      return tail;
    }
  }

  // Shares the hazelnut-coated tail with Hazel Amour and Bananarama.
  public class BeaverTailPeanutCoatedItem : CustomItemGroup {
    public const string NameId = "beavertail_peanut_coated";

    // Lighter than the hazelnut coat it sits on, so the two stages read apart.
    public static Color Colour => new Color( 0.50f, 0.33f, 0.16f );

    public override string UniqueNameID => NameId;

    private GameObject prefab;

    public override GameObject Prefab {
      get => prefab ?? ( prefab = Gdo.CloneFromBundle(
          "BeaverTail", "Beaver Tail - Peanut Coated", Colour ));
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
                        Gdo.Own<Item>(BeaverTailHazelnutCoatedItem.NameId),
                        Gdo.Own<Item>(PeanutButterItem.NameId),
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
                    Item = Gdo.Own<Item>(PeanutButterItem.NameId),
                    Text = "PC",
                },
            };
      protected set { }
    }

    public override ItemValue ItemValue {
      get => ItemValue.Small;
      protected set { }
    }

    public override string ColourBlindTag {
      get => "PC";
      protected set { }
    }
  }

  // Triple Trip: two spreads and a scatter of Reese's Pieces.
  public class BeaverTailTripleTripItem : CustomItemGroup {
    public const string NameId = "beavertail_triple_trip";

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
                    IsMandatory = true,
                    Items = new List<Item>
                    {
                        Gdo.Own<Item>(BeaverTailPeanutCoatedItem.NameId),
                        Gdo.Own<Item>(ReesesPiecesItem.NameId),
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
                    Item = Gdo.Own<Item>(ReesesPiecesItem.NameId),
                    Text = "TT",
                },
            };
      protected set { }
    }

    public override ItemValue ItemValue {
      get => ItemValue.Small;
      protected set { }
    }

    public override string ColourBlindTag {
      get => "TT";
      protected set { }
    }

    // Simple Flat has one _Color0 per material, so mixed candy needs a mesh per colour.
    private static GameObject BuildPrefab() {
      var tail = Gdo.CloneFromBundle(
          "BeaverTail", "Beaver Tail - Triple Trip", BeaverTailPeanutCoatedItem.Colour );
      if ( tail == null ) {
        return null;
      }

      var candy = new (string Asset, Color Colour)[]
      {
                ("CandyOrange", ReesesPiecesItem.CandyOrange),
                ("CandyYellow", new Color(0.96f, 0.76f, 0.13f)),
                ("CandyBrown", new Color(0.40f, 0.22f, 0.11f)),
      };

      foreach ( var (asset, colour) in candy ) {
        var pieces = Gdo.CloneFromBundle( asset, asset, colour );
        if ( pieces == null ) {
          continue;
        }

        pieces.transform.SetParent( tail.transform, false );
        pieces.transform.localPosition = Vector3.zero;
      }

      return tail;
    }
  }

  // Avalanche, step one: cheesecake spread on a fried tail.
  public class BeaverTailCheesecakeCoatedItem : CustomItemGroup {
    public const string NameId = "beavertail_cheesecake_coated";

    public static Color Colour => new Color( 0.93f, 0.83f, 0.66f );

    public override string UniqueNameID => NameId;

    private GameObject prefab;

    public override GameObject Prefab {
      get => prefab ?? ( prefab = Gdo.CloneFromBundle(
          "BeaverTail", "Beaver Tail - Cheesecake Coated", Colour ));
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
                        Gdo.Own<Item>(BeaverTailCookedItem.NameId),
                        Gdo.Own<Item>(CheesecakeSpreadItem.NameId),
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
                    Item = Gdo.Own<Item>(CheesecakeSpreadItem.NameId),
                    Text = "CC",
                },
            };
      protected set { }
    }

    public override ItemValue ItemValue {
      get => ItemValue.Small;
      protected set { }
    }

    public override string ColourBlindTag {
      get => "CC";
      protected set { }
    }
  }

  // Avalanche, step two: Skor bits over the spread.
  public class BeaverTailSkorItem : CustomItemGroup {
    public const string NameId = "beavertail_skor";

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
                    IsMandatory = true,
                    Items = new List<Item>
                    {
                        Gdo.Own<Item>(BeaverTailCheesecakeCoatedItem.NameId),
                        Gdo.Own<Item>(SkorBitsItem.NameId),
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
                    Item = Gdo.Own<Item>(SkorBitsItem.NameId),
                    Text = "Sk",
                },
            };
      protected set { }
    }

    public override ItemValue ItemValue {
      get => ItemValue.Small;
      protected set { }
    }

    public override string ColourBlindTag {
      get => "AS";
      protected set { }
    }

    private static GameObject BuildPrefab() {
      var tail = Gdo.CloneFromBundle(
          "BeaverTail", "Beaver Tail - Skor", BeaverTailCheesecakeCoatedItem.Colour );
      if ( tail == null ) {
        return null;
      }

      var bits = Gdo.CloneFromBundle( "SkorScatter", "Skor Bits", SkorBitsItem.Toffee );
      if ( bits != null ) {
        bits.transform.SetParent( tail.transform, false );
        bits.transform.localPosition = Vector3.zero;
      }

      return tail;
    }
  }

  // Finished with the caramel already cooked for Apple Pie.
  public class BeaverTailAvalancheItem : CustomItemGroup {
    public const string NameId = "beavertail_avalanche";

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
                    IsMandatory = true,
                    Items = new List<Item>
                    {
                        Gdo.Own<Item>(BeaverTailSkorItem.NameId),
                        Gdo.Lib(Gdo.LibKeys.Caramel),
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
                    Item = Gdo.Lib(Gdo.LibKeys.Caramel),
                    Text = "Av",
                },
            };
      protected set { }
    }

    public override ItemValue ItemValue {
      get => ItemValue.Small;
      protected set { }
    }

    public override string ColourBlindTag {
      get => "Av";
      protected set { }
    }

    private static GameObject BuildPrefab() {
      var tail = Gdo.CloneFromBundle(
          "BeaverTail", "Beaver Tail - Avalanche", BeaverTailCheesecakeCoatedItem.Colour );
      if ( tail == null ) {
        return null;
      }

      var bits = Gdo.CloneFromBundle( "SkorScatter", "Skor Bits", SkorBitsItem.Toffee );
      if ( bits != null ) {
        bits.transform.SetParent( tail.transform, false );
        bits.transform.localPosition = Vector3.zero;
      }

      var drizzle = Gdo.CloneFromBundle(
          "DrizzleRibbon", "Caramel Drizzle", PotWithCaramelItem.Amber );
      if ( drizzle != null ) {
        drizzle.transform.SetParent( tail.transform, false );
        // DrizzleRibbon is authored clear of the topping layer, so it needs no lift.
        drizzle.transform.localPosition = Vector3.zero;
      }

      return tail;
    }
  }

  // mEHple, step one: maple butter over a fried tail.
  public class BeaverTailMapleButteredItem : CustomItemGroup {
    public const string NameId = "beavertail_maple_buttered";

    public override string UniqueNameID => NameId;

    private GameObject prefab;

    public override GameObject Prefab {
      get => prefab ?? ( prefab = Gdo.CloneFromBundle(
          "BeaverTail", "Beaver Tail - Maple Buttered", MapleButterItem.Colour ));
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
                        Gdo.Own<Item>(BeaverTailCookedItem.NameId),
                        Gdo.Own<Item>(MapleButterItem.NameId),
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
                    Item = Gdo.Own<Item>(MapleButterItem.NameId),
                    Text = "MB",
                },
            };
      protected set { }
    }

    public override ItemValue ItemValue {
      get => ItemValue.Small;
      protected set { }
    }

    public override string ColourBlindTag {
      get => "MT";
      protected set { }
    }
  }

  // mEHple: maple butter finished with maple sugar.
  public class BeaverTailMehpleItem : CustomItemGroup {
    public const string NameId = "beavertail_mehple";

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
                    IsMandatory = true,
                    Items = new List<Item>
                    {
                        Gdo.Own<Item>(BeaverTailMapleButteredItem.NameId),
                        Gdo.Own<Item>(MapleSugarItem.NameId),
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
                    Item = Gdo.Own<Item>(MapleSugarItem.NameId),
                    Text = "Mp",
                },
            };
      protected set { }
    }

    public override ItemValue ItemValue {
      get => ItemValue.Small;
      protected set { }
    }

    public override string ColourBlindTag {
      get => "Mp";
      protected set { }
    }

    // the sugar-dusting scatter retinted amber: maple sugar, not icing sugar
    private static GameObject BuildPrefab() {
      var tail = Gdo.CloneFromBundle(
          "BeaverTail", "Beaver Tail - mEHple", MapleButterItem.Colour );
      if ( tail == null ) {
        return null;
      }

      var crunch = Gdo.CloneFromBundle(
          "SugarDusting", "Maple Sugar", MapleSugarItem.Colour );
      if ( crunch != null ) {
        crunch.transform.SetParent( tail.transform, false );
        crunch.transform.localPosition = Vector3.zero;
      }

      return tail;
    }
  }
}
