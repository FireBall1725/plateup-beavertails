// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2026 FireBall1725

using System.Collections.Generic;
using KitchenData;
using KitchenLib.Customs;
using KitchenLib.References;
using UnityEngine;

namespace BeaverTails {
  // What comes out of the fryer, using FryMe rather than the old pot-and-oil groups, which could not burn because an item cannot have both a split and a process.

  // What comes out of the fryer.
  public class BeaverTailCookedItem : CustomItem {
    public const string NameId = "beavertail_cooked";

    public override string UniqueNameID => NameId;

    private GameObject prefab;

    public override GameObject Prefab {
      get => prefab ?? ( prefab = Gdo.CloneFromBundle(
          "BeaverTail", "Beaver Tail", new Color( 0.72f, 0.47f, 0.22f )));
      protected set { }
    }

    public override string ColourBlindTag {
      get => "BT";
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

    // Leave it in the fryer and it ruins, which works only because the cooked tail is a plain item rather than a split from a pot.
    public override List<Item.ItemProcess> Processes {
      get => new List<Item.ItemProcess>
      {
                new Item.ItemProcess
                {
                    Process = Gdo.FryProcess(),
                    Result = Gdo.Own<Item>(BeaverTailBurnedItem.NameId),
                    Duration = 20,
                    IsBad = true,
                },
            };
      protected set { }
    }

    // Prep stations, freezers and fridges reject an item whose ItemStorageFlags is None, the default for a custom item.
    public override void OnRegister( Item gameDataObject ) {
      base.OnRegister( gameDataObject );
      Gdo.AllowStorage( gameDataObject );
    }
  }

  public class BeaverTailBurnedItem : CustomItem {
    public const string NameId = "beavertail_burned";

    public override string UniqueNameID => NameId;

    private GameObject prefab;

    public override GameObject Prefab {
      get => prefab ?? ( prefab = Gdo.CloneFromBundle(
          "BeaverTail", "Beaver Tail - Burned", new Color( 0.13f, 0.10f, 0.08f )));
      protected set { }
    }

    public override string ColourBlindTag {
      get => "X";
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
}
