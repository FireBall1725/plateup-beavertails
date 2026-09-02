// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2026 FireBall1725

using Kitchen;
using UnityEngine;

namespace BeaverTails {
  // Shows what is left in a jar or pot by shortening a bar, subclassing SplittableItemView so the game's own system drives it.
  public class SplitLevelView : SplittableItemView {
    // assigned by Gdo.AddLevelBar when the prefab is built
    public GameObject Bar;
    public float BarFullLength;
    public float BarLeftX;
    public float BarY;
    public float BarZ;

    protected override void UpdateData( ViewData data ) {
      if ( Bar == null || BarFullLength <= 0f ) {
        return;
      }

      // Total is 0 on an item that is not splittable yet; treat that as full.
      var fraction = data.Total > 0 ? Mathf.Clamp01( data.Remaining / ( float )data.Total ) : 1f;

      Bar.SetActive( fraction > 0.001f );
      if ( fraction <= 0.001f ) {
        return;
      }

      // A floor on the length so the last serving is still a visible stub.
      var length = Mathf.Max( BarFullLength * fraction, BarFullLength * 0.07f );

      var scale = Bar.transform.localScale;
      Bar.transform.localScale = new Vector3( length, scale.y, scale.z );

      // Scaling a cube grows it about its centre, so the bar is pushed right by half of what it lost.
      Bar.transform.localPosition = new Vector3( BarLeftX + ( length * 0.5f ), BarY, BarZ );
    }
  }
}
