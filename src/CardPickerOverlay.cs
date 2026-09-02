// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2026 FireBall1725

using System.Collections.Generic;
using UnityEngine;

namespace BeaverTails {
  // The card list; drawing and input live in PickerOverlayBase.
  public class CardPickerOverlay : PickerOverlayBase {
    internal CardPickerSystem Owner;

    private bool oursOnly = true;

    protected override string Title => "Beaver Tails card picker            F3 to close";

    protected override string Hint =>
        "enter grant   del remove   up/down move   pgup/pgdn page";

    protected override bool CanAct => Owner != null && Owner.IsHost();

    protected override string BlockedMessage =>
        "CLIENT: granting is disabled, progression is host authoritative";

    protected override void DrawExtraControls() {
      var toggled = GUILayout.Toggle( oursOnly, " ours only", RowStyle );
      if ( toggled != oursOnly ) {
        oursOnly = toggled;
        MarkDirty();
      }
    }

    protected override List<PickerRow> BuildRows( string filter ) {
      var rows = new List<PickerRow>();
      if ( Owner == null ) {
        return rows;
      }

      foreach ( var card in Owner.BuildCards( filter, oursOnly )) {
        rows.Add( new PickerRow {
          Id = card.Id,
          Left = card.Name,
          Middle = card.Kind,
          Right = ( card.Difficulty > 0 ? $"{card.Difficulty}*" : "  " )
                    + ( card.IsHeld ? "   HELD" : string.Empty ),
        } );
      }

      return rows;
    }

    protected override string Activate( PickerRow row ) => Owner?.GrantById( row.Id );

    protected override string Secondary( PickerRow row ) => Owner?.RemoveById( row.Id );
  }
}
