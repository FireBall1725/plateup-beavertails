// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2026 FireBall1725

using Kitchen;
using KitchenMods;
using Unity.Entities;
using UnityEngine;

namespace BeaverTails {
  // F5 ends the day on the spot so a real card offer can be tested, running after AdvanceTime so SIsNightFirstUpdate gets its usual one-frame life.
  [UpdateInGroup( typeof( TimeManagementGroup ))]
  [UpdateAfter( typeof( AdvanceTime ))]
  public class ForceEndOfDaySystem : GameSystemBase, IModSystem {
    private const KeyCode EndDayKey = KeyCode.F5;

    private EntityQuery dayTime;
    private EntityQuery timeRead;
    private EntityQuery timeWrite;

    protected override void Initialise() {
      base.Initialise();
      dayTime = GetEntityQuery( ComponentType.ReadOnly<SIsDayTime>());
      timeRead = GetEntityQuery( ComponentType.ReadOnly<STime>());
      timeWrite = GetEntityQuery( ComponentType.ReadWrite<STime>());
    }

    protected override void OnUpdate() {
      if ( !Input.GetKeyDown( EndDayKey )) {
        return;
      }

      // The same two states AdvanceTime refuses to run in.
      if ( Has<SGameOver>() || Has<SPracticeMode>()) {
        Debug.Log( "[BeaverTails] F5 ignored: game over or practice mode" );
        return;
      }

      if ( !Has<SIsDayTime>()) {
        Debug.Log( "[BeaverTails] F5 ignored: already night" );
        return;
      }

      BecomeNight();
      Debug.Log( "[BeaverTails] day forced to night, card options follow" );
    }

    // AdvanceTime.BecomeNight copied, including the clock reset, because nothing else resets STime.
    private void BecomeNight() {
      var current = timeRead.GetSingleton<STime>();

      EntityManager.CreateEntity( typeof( SIsNightFirstUpdate ));
      if ( !HasSingleton<SIsNightTime>()) {
        EntityManager.CreateEntity( typeof( SIsNightTime ));
      }

      if ( HasSingleton<SIsDayTime>()) {
        EntityManager.RemoveComponent<SIsDayTime>( dayTime.GetSingletonEntity());
      }

      timeWrite.SetSingleton( new STime { DayLength = current.DayLength } );
    }
  }
}
