// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2026 FireBall1725

using System.Collections.Generic;
using Kitchen;
using KitchenData;
using KitchenLib.Customs;
using KitchenLib.References;
using UnityEngine;

namespace BeaverTails {
  // Unused: a sleeve cannot carry an order ticket, so tails are served on plates.

  // The mesh comes from our bundle, which carries geometry only, so the material is applied
  // here from PlateUp's Simple Flat shader.
  public class PaperSleeveItem : CustomItem {
    public const string NameId = "paper_sleeve";

    public override string UniqueNameID => NameId;

    private GameObject prefab;

    public override GameObject Prefab {
      get => prefab ?? ( prefab = Gdo.CloneFromBundle(
          "PaperSleeve", "Paper Sleeve", new Color( 0.72f, 0.09f, 0.10f )));
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

  // Borrows the Pot Stack's look, since it is already a provider that sits on a counter.
  public class SleeveStackAppliance : CustomAppliance {
    public const string NameId = "sleeve_stack";

    public override string UniqueNameID => NameId;

    private GameObject prefab;

    public override GameObject Prefab {
      get => prefab ?? ( prefab = Gdo.CloneAppliancePrefab(
          ApplianceReferences.PotStack,
          "Sleeve Stack",
          new Color( 0.72f, 0.09f, 0.10f )));
      protected set { }
    }

    public override bool IsPurchasable {
      get => false;
      protected set { }
    }

    public override List<IApplianceProperty> Properties {
      get => new List<IApplianceProperty>
      {
                new CItemProvider
                {
                    ProvidedItem = Gdo.Own<Item>(PaperSleeveItem.NameId)?.ID ?? 0,
                    Maximum = 4,
                    Available = 4,
                    AllowRefreshes = true,
                    EmptyAtNight = false,
                },
            };
      protected set { }
    }
  }

  // A sugared tail in a sleeve, which is what the customer orders.
  public class BeaverTailServedItem : CustomItemGroup {
    public const string NameId = "beavertail_served";

    public override string UniqueNameID => NameId;

    private GameObject prefab;

    public override GameObject Prefab {
      get => prefab ?? ( prefab = BuildServedPrefab());
      protected set { }
    }

    // A served tail always contains exactly one tail, so building the combined look directly
    // avoids ItemGroupView's show/hide machinery and fixes the order ticket.
    private static GameObject BuildServedPrefab() {
      var sleeve = Gdo.CloneFromBundle(
          "PaperSleeve", "Beaver Tail - Served", new Color( 0.72f, 0.09f, 0.10f ));
      if ( sleeve == null ) {
        return null;
      }

      // lay it down instead of standing it upright
      for ( var i = 0; i < sleeve.transform.childCount; i++ ) {
        sleeve.transform.GetChild( i ).localRotation = Quaternion.Euler( 72f, 0f, 0f );
      }

      var tail = Gdo.ClonePrefab(
          ItemReferences.Dough,
          "Served Tail",
          scale: new Vector3( 1.7f, 0.22f, 1.0f ),
          tint: new Color( 0.80f, 0.60f, 0.36f ));

      if ( tail != null ) {
        tail.transform.SetParent( sleeve.transform, false );
        tail.transform.localPosition = new Vector3( 0f, 0.10f, 0.10f );
        tail.transform.localScale = Vector3.one * 0.30f;
      }

      return sleeve;
    }

    // Off, or KitchenLib attaches an ItemGroupView that hides every child it does not
    // recognise.
    public override bool AutoSetupItemGroupView {
      get => false;
      protected set { }
    }

    public override ItemValue ItemValue {
      get => ItemValue.Medium;
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
                        Gdo.Own<Item>(PaperSleeveItem.NameId),
                    },
                },
            };
      protected set { }
    }
  }
}
