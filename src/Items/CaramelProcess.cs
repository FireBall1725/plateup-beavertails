// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2026 FireBall1725

using System.Collections.Generic;
using KitchenData;
using KitchenLib.Customs;
using Kitchen;
using KitchenLib.References;
using KitchenMods;
using UnityEngine;

namespace BeaverTails {
  // A hob-only heat process, because the Fryer also provides Cook and would otherwise satisfy the requirement without the kitchen ever being handed a hob.
  public class CaramelizeProcess : CustomProcess {
    public const string NameId = "caramelise";

    public override string UniqueNameID => NameId;

    // Cook, so the shop stocks hobs: the Hob requires Cook, and nothing else in a run asks for it.
    public override Process IsPseudoprocessFor {
      get => Gdo.Process( ProcessReferences.Cook );
      protected set { }
    }

    // this is what the game hands you when a dish requires the process
    public override GameDataObject BasicEnablingAppliance {
      get => Gdo.Appliance( ApplianceReferences.Hob );
      protected set { }
    }

    private List<(Locale, ProcessInfo)> cachedInfo;

    public override List<(Locale, ProcessInfo)> InfoList {
      // A TMP sprite tag, because a bare character draws as a tilde adrift in its circle.
      get => cachedInfo ?? ( cachedInfo = new List<(Locale, ProcessInfo)>
      {
                (Locale.English, Gdo.ProcessInfo("Caramelise", "<sprite name=\"cook\">")),
            } );
      protected set { }
    }

    // Teach the hobs during registration; the appliance-to-process index is built once during setup.
    public override void OnRegister( Process gameDataObject ) {
      base.OnRegister( gameDataObject );

      Gdo.TeachProcess(
          gameDataObject,
          new[]
          {
                    ApplianceReferences.Hob,
                    ApplianceReferences.HobStarting,
                    ApplianceReferences.HobSafe,
                    ApplianceReferences.HobDanger,
                    ApplianceReferences.ManualHob,
                    ApplianceReferences.TutorialHob,
                    // Not TableSharingCauldron, which is a dining table rather than a cooker.
                    ApplianceReferences.Cauldron,
          },
          "caramelise" );
    }
  }
}
