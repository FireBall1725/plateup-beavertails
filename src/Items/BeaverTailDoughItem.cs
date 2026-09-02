// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2026 FireBall1725

using System.Collections.Generic;
using KitchenData;
using KitchenLib.Customs;
using KitchenLib.References;

namespace BeaverTails {
  // Our own Flour + Milk group, separate from base Dough because that one has already spent Knead on Pie Crust and Cook on Bread.
  public class BeaverTailDoughItem : CustomItemGroup {
    public const string NameId = "beavertail_dough";

    public override string UniqueNameID => NameId;

    private UnityEngine.GameObject prefab;

    public override UnityEngine.GameObject Prefab {
      get => prefab ?? ( prefab = Gdo.ClonePrefab( ItemReferences.Dough, "Beaver Tail Dough" ));
      protected set { }
    }

    public override string ColourBlindTag {
      get => "BD";
      protected set { }
    }

    public override ItemValue ItemValue {
      get => ItemValue.Small;
      protected set { }
    }

    public override List<ItemGroup.ItemSet> Sets {
      get => new List<ItemGroup.ItemSet>
      {
                // A set lists alternatives, so accepting base Dough alongside Flour lets a cook rescue the wrong dough with milk.
                new ItemGroup.ItemSet
                {
                    Min = 1,
                    Max = 1,
                    IsMandatory = false,
                    Items = new List<Item>
                    {
                        Gdo.Item(ItemReferences.Flour),
                        Gdo.Item(ItemReferences.Dough),
                    },
                },
                new ItemGroup.ItemSet
                {
                    Min = 1,
                    Max = 1,
                    IsMandatory = false,
                    Items = new List<Item> { Gdo.Item(ItemReferences.Milk) },
                },
            };
      protected set { }
    }

    public override List<Item.ItemProcess> Processes {
      get => new List<Item.ItemProcess>
      {
                new Item.ItemProcess
                {
                    Process = Gdo.Process(ProcessReferences.Knead),
                    Result = Gdo.Own<Item>(BeaverTailRawItem.NameId),
                    Duration = 2,
                },
            };
      protected set { }
    }

    // Prep stations, freezers and fridges reject an item whose ItemStorageFlags is None, the default for a custom item.
    public override void OnRegister( ItemGroup gameDataObject ) {
      base.OnRegister( gameDataObject );
      Gdo.AllowStorage( gameDataObject );
    }
  }
}
