// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2026 FireBall1725

using System.Collections.Generic;

namespace BeaverTails {
  // The appliance spawner, limited to this mod's appliances plus the storage ones worth testing against.
  public class AppliancePickerOverlay : PickerOverlayBase {
    internal AppliancePickerSystem Owner;

    protected override string Title => "Beaver Tails appliance spawner      F4 to close";

    protected override string Hint => "enter spawn in front of you   up/down move";

    protected override bool CanAct => Owner != null && Owner.IsHost();

    protected override string BlockedMessage => "CLIENT: spawning is disabled, host only";

    protected override List<PickerRow> BuildRows( string filter ) {
      var rows = new List<PickerRow>();
      if ( Owner == null ) {
        return rows;
      }

      foreach ( var appliance in Owner.BuildAppliances( filter )) {
        rows.Add( new PickerRow {
          Id = appliance.Id,
          Left = appliance.Name,
          Middle = appliance.Source,
          Right = string.Empty,
        } );
      }

      return rows;
    }

    protected override string Activate( PickerRow row ) => Owner?.Spawn( row.Id, row.Left );
  }
}
