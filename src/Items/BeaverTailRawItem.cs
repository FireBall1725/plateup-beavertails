// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2026 FireBall1725

using System.Collections.Generic;
using KitchenData;
using KitchenLib.Customs;
using KitchenLib.References;
using KitchenLib.Utils;
using UnityEngine;

namespace BeaverTails {
  // Flattened raw dough ready for the oil, borrowing Doughnut - Raw's prefab so this needs no AssetBundle.
  public class BeaverTailRawItem : CustomItem {
    public const string NameId = "beavertail_raw";

    public override string UniqueNameID => NameId;

    private GameObject prefab;

    // Squashed flat and stretched long so a kneaded tail does not look like the ball it came from.
    public override GameObject Prefab {
      get => prefab ?? ( prefab = Gdo.CloneFromBundle(
          "BeaverTail", "Beaver Tail - Raw", new Color( 0.93f, 0.86f, 0.70f )));
      protected set { }
    }

    public override string ColourBlindTag {
      get => "R";
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

    // FryMe comes from the Fryer Appliance mod, so this cannot be cooked on a bare hob.
    public override List<Item.ItemProcess> Processes {
      get => new List<Item.ItemProcess>
      {
                new Item.ItemProcess
                {
                    Process = Gdo.FryProcess(),
                    Result = Gdo.Own<Item>(BeaverTailCookedItem.NameId),
                    Duration = 4,
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
}
