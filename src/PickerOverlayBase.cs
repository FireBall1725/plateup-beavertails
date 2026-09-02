// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2026 FireBall1725

using System.Collections.Generic;
using UnityEngine;

namespace BeaverTails {
  // One line in a picker list: an id to act on, and three columns to draw.
  public struct PickerRow {
    public int Id;
    public string Left;
    public string Middle;
    public string Right;
  }

  // Shared drawing and input for the mod's overlays, on IMGUI rather than PlateUp's menu system so no game update can break it.
  public abstract class PickerOverlayBase : MonoBehaviour {
    protected const int Rows = 14;

    private bool open;
    private string filter = string.Empty;
    private int selected;
    private int scroll;
    private string status = string.Empty;
    private List<PickerRow> rows = new List<PickerRow>();

    private GUIStyle rowStyle;
    private GUIStyle selectedStyle;
    private GUIStyle headerStyle;

    protected abstract string Title { get; }

    protected abstract string Hint { get; }

    protected abstract List<PickerRow> BuildRows( string filter );

    protected abstract string Activate( PickerRow row );

    protected virtual string Secondary( PickerRow row ) => null;

    protected virtual void DrawExtraControls() {
    }

    protected virtual bool CanAct => true;

    protected virtual string BlockedMessage => string.Empty;

    internal void Toggle() {
      open = !open;
      status = string.Empty;
    }

    protected void MarkDirty() {
      selected = 0;
      scroll = 0;
    }

    private void Update() {
      if ( !open ) {
        return;
      }

      // Rebuilt every frame so a card granted elsewhere shows up without the list going stale.
      rows = BuildRows( filter );
      selected = Mathf.Clamp( selected, 0, Mathf.Max( 0, rows.Count - 1 ));
    }

    private void OnGUI() {
      if ( !open ) {
        return;
      }

      EnsureStyles();
      HandleKeys();

      var width = 620f;
      var height = 90f + ( Rows * 22f ) + 46f;
      var area = new Rect(
          ( Screen.width - width ) / 2f, ( Screen.height - height ) / 2f, width, height );

      GUI.Box( area, GUIContent.none );
      GUILayout.BeginArea( new Rect( area.x + 12f, area.y + 10f, area.width - 24f, area.height - 20f ));

      GUILayout.Label( Title, headerStyle );

      GUILayout.BeginHorizontal();
      GUILayout.Label( "filter:", rowStyle, GUILayout.Width( 48f ));
      GUI.SetNextControlName( "filter" );
      var typed = GUILayout.TextField( filter, GUILayout.Width( 220f ));
      if ( typed != filter ) {
        filter = typed;
        MarkDirty();
      }

      GUILayout.Space( 16f );
      DrawExtraControls();
      GUILayout.FlexibleSpace();
      GUILayout.Label( $"{rows.Count}", rowStyle );
      GUILayout.EndHorizontal();

      GUILayout.Space( 6f );
      DrawRows();
      GUILayout.Space( 6f );

      GUILayout.Label( CanAct ? Hint : BlockedMessage, rowStyle );

      if ( !string.IsNullOrEmpty( status )) {
        GUILayout.Label( status, rowStyle );
      }

      GUILayout.EndArea();
    }

    private void DrawRows() {
      if ( rows.Count == 0 ) {
        GUILayout.Label( "  nothing matches", rowStyle );
        return;
      }

      scroll = Mathf.Clamp( scroll, Mathf.Max( 0, selected - Rows + 1 ), Mathf.Max( 0, selected ));
      scroll = Mathf.Min( scroll, Mathf.Max( 0, rows.Count - Rows ));

      for ( var i = scroll; i < Mathf.Min( rows.Count, scroll + Rows ); i++ ) {
        var row = rows[i];
        var marker = i == selected ? ">" : " ";
        GUILayout.Label(
            $"{marker} {Pad( row.Left, 34 )} {Pad( row.Middle, 12 )} {row.Right}",
            i == selected ? selectedStyle : rowStyle );
      }
    }

    protected static string Pad( string value, int width ) {
      value = value ?? string.Empty;
      return value.Length >= width ? value.Substring( 0, width ) : value.PadRight( width );
    }

    private void HandleKeys() {
      var e = Event.current;
      if ( e == null || e.type != EventType.KeyDown || rows.Count == 0 ) {
        return;
      }

      var typing = GUI.GetNameOfFocusedControl() == "filter";

      switch ( e.keyCode ) {
        case KeyCode.DownArrow:
          selected = Mathf.Min( rows.Count - 1, selected + 1 );
          e.Use();
          break;
        case KeyCode.UpArrow:
          selected = Mathf.Max( 0, selected - 1 );
          e.Use();
          break;
        case KeyCode.PageDown:
          selected = Mathf.Min( rows.Count - 1, selected + Rows );
          e.Use();
          break;
        case KeyCode.PageUp:
          selected = Mathf.Max( 0, selected - Rows );
          e.Use();
          break;
        case KeyCode.Return:
        case KeyCode.KeypadEnter:
          status = Activate( rows[selected] ) ?? string.Empty;
          e.Use();
          break;
        case KeyCode.Delete:
        case KeyCode.Backspace when !typing:
          status = Secondary( rows[selected] ) ?? string.Empty;
          e.Use();
          break;
      }
    }

    private void EnsureStyles() {
      if ( rowStyle != null ) {
        return;
      }

      rowStyle = new GUIStyle( GUI.skin.label ) {
        font = Font.CreateDynamicFontFromOSFont( "Menlo", 13 ),
        richText = false,
      };
      rowStyle.normal.textColor = new Color( 0.88f, 0.88f, 0.90f );

      selectedStyle = new GUIStyle( rowStyle );
      selectedStyle.normal.textColor = new Color( 1f, 0.82f, 0.35f );

      headerStyle = new GUIStyle( rowStyle );
      headerStyle.normal.textColor = Color.white;
      headerStyle.fontStyle = FontStyle.Bold;
    }

    protected GUIStyle RowStyle => rowStyle;
  }
}
