// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2026 FireBall1725

using System.Linq;
using System.Reflection;
using KitchenLib;
using KitchenMods;
using UnityEngine;

namespace BeaverTails {
  // The mod entry point and the one place every AddGameDataObject call lives, so registration order is readable top to bottom.
  public class BeaverTailsMod : BaseMod, IModSystem {
    public const string Guid = "fireball1725.plateup.beavertails";
    public const string DisplayName = "Beaver Tails";
    public const string Author = "FireBall1725";
    // Comes from the csproj's Version, which CI sets from the tag.
    public static readonly string Version = ReadVersion();

    // Strips the +<sha> that AssemblyInformationalVersion carries.
    private static string ReadVersion() {
      var attr = Assembly.GetExecutingAssembly()
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>();
      var v = attr?.InformationalVersion;
      if ( string.IsNullOrEmpty( v )) {
        return "0.0.0-dev";
      }
      var plus = v.IndexOf( '+' );
      return plus < 0 ? v : v.Substring( 0, plus );
    }

    // PlateUp 1.5.1-5635 at time of writing
    public const string CompatibleGameVersions = ">=1.5.0";

    public BeaverTailsMod()
        : base( Guid, DisplayName, Author, Version, CompatibleGameVersions, Assembly.GetExecutingAssembly()) {
    }

    protected override void OnInitialise() {
      Log( $"{DisplayName} v{Version} initialising, build {BuildStamp()}" );
    }

    // The host's copy decides which cards can be offered, so compare this stamp across both players' Player.log before debugging anything else.
    internal static string BuildStamp() {
      try {
        var path = Assembly.GetExecutingAssembly().Location;
        if ( string.IsNullOrEmpty( path )) {
          return "unknown";
        }

        return System.IO.File.GetLastWriteTimeUtc( path ).ToString( "yyyy-MM-dd HH:mm" ) + " UTC";
      } catch ( System.Exception ) {
        return "unknown";
      }
    }

    // The loader picks up any .assets file next to the DLL as an AssetBundleModPack.
    public static AssetBundle Bundle;

    protected override void OnPostActivate( Mod mod ) {
      Bundle = mod.GetPacks<AssetBundleModPack>().SelectMany( pack => pack.AssetBundles ).FirstOrDefault();
      Log( Bundle == null ? "NO asset bundle found" : $"asset bundle loaded: {Bundle.name}" );

      // Order matters: every group resolves its components by name at Convert time.

      // Items before their appliances, because the appliance resolves the item's ID for CItemProvider.
      AddGameDataObject<HazelnutSpreadItem>();
      AddGameDataObject<PeanutButterItem>();
      AddGameDataObject<CheesecakeSpreadItem>();
      AddGameDataObject<SkorBitsItem>();
      AddGameDataObject<ReesesPiecesItem>();
      AddGameDataObject<PistachioCrumbsItem>();
      AddGameDataObject<CrushedOreoItem>();
      AddGameDataObject<WhiteChocolateChunksItem>();
      AddGameDataObject<PistachioSpreadItem>();
      AddGameDataObject<VanillaIcingItem>();
      AddGameDataObject<MapleButterItem>();
      AddGameDataObject<CinnamonSugarItem>();
      AddGameDataObject<HazelnutJarItem>();
      AddGameDataObject<PeanutJarItem>();
      AddGameDataObject<CheesecakeJarItem>();
      AddGameDataObject<PistachioJarItem>();
      AddGameDataObject<VanillaIcingJarItem>();
      AddGameDataObject<CaramelizeProcess>();
      AddGameDataObject<PotWithBurnedItem>();
      // poutine: portions, then the cooked pots, then the raw pots
      AddGameDataObject<GravyItem>();
      AddGameDataObject<CheeseCurdsItem>();
      AddGameDataObject<PotWithGravyItem>();
      AddGameDataObject<PotWithCurdsItem>();
      AddGameDataObject<PotWithGravyRawItem>();
      AddGameDataObject<PotWithMilkItem>();
      AddGameDataObject<FriedCheeseCurdsItem>();
      AddGameDataObject<PoutineBaseItem>();
      AddGameDataObject<PoutineItem>();
      AddGameDataObject<DoubleCheesePoutineItem>();
      AddGameDataObject<PotWithCaramelItem>();
      AddGameDataObject<ApplePieFillingItem>();
      AddGameDataObject<PotWithAppleFillingItem>();
      AddGameDataObject<MapleSugarItem>();
      AddGameDataObject<PotWithMapleSugarItem>();
      AddGameDataObject<PotWithSugarItem>();
      AddGameDataObject<PotWithAppleItem>();
      AddGameDataObject<PotWithMapleItem>();
      AddGameDataObject<JarStackAppliance>();
      AddGameDataObject<PeanutJarStackAppliance>();
      AddGameDataObject<CheesecakeJarStackAppliance>();
      AddGameDataObject<PistachioJarStackAppliance>();
      AddGameDataObject<VanillaIcingJarStackAppliance>();
      AddGameDataObject<SkorBinAppliance>();
      AddGameDataObject<ReesesBinAppliance>();
      AddGameDataObject<PistachioBinAppliance>();
      AddGameDataObject<OreoBinAppliance>();
      AddGameDataObject<WhiteChocolateBinAppliance>();

      // the fried chain
      AddGameDataObject<BeaverTailBurnedItem>();
      AddGameDataObject<BeaverTailCookedItem>();
      AddGameDataObject<BeaverTailRawItem>();
      AddGameDataObject<BeaverTailDoughItem>();

      // named recipes, each stage before anything that builds on it
      AddGameDataObject<BeaverTailClassicItem>();
      AddGameDataObject<BeaverTailKillaloeItem>();
      AddGameDataObject<BeaverTailPistachioCoatedItem>();
      AddGameDataObject<BeaverTailPistachioHoneyItem>();
      AddGameDataObject<BeaverTailPistachOhItem>();
      AddGameDataObject<BeaverTailHazelnutCoatedItem>();
      AddGameDataObject<BeaverTailMapleButteredItem>();
      AddGameDataObject<BeaverTailMehpleItem>();
      AddGameDataObject<BeaverTailCheesecakeCoatedItem>();
      AddGameDataObject<BeaverTailBrownieItem>();
      AddGameDataObject<BeaverTailBrwownieItem>();
      AddGameDataObject<BeaverTailVanillaCoatedItem>();
      AddGameDataObject<BeaverTailVanillaOreoItem>();
      AddGameDataObject<BeaverTailCocoVanilItem>();
      AddGameDataObject<BeaverTailCheesecakeJamItem>();
      AddGameDataObject<BeaverTailStrawberryCheesecakeItem>();
      AddGameDataObject<BeaverTailSkorItem>();
      AddGameDataObject<BeaverTailAvalancheItem>();
      AddGameDataObject<BeaverTailPeanutCoatedItem>();
      AddGameDataObject<BeaverTailTripleTripItem>();
      AddGameDataObject<BeaverTailHazelAmourItem>();
      AddGameDataObject<BeaverTailBananaramaItem>();
      AddGameDataObject<BeaverTailAppleItem>();
      AddGameDataObject<BeaverTailApplePieItem>();

      // one plated menu item per recipe
      AddGameDataObject<BeaverTailClassicPlatedItem>();
      AddGameDataObject<BeaverTailKillaloePlatedItem>();
      AddGameDataObject<BeaverTailHazelAmourPlatedItem>();
      AddGameDataObject<BeaverTailBananaramaPlatedItem>();
      AddGameDataObject<BeaverTailApplePiePlatedItem>();
      AddGameDataObject<BeaverTailTripleTripPlatedItem>();
      AddGameDataObject<BeaverTailAvalanchePlatedItem>();
      AddGameDataObject<BeaverTailMehplePlatedItem>();
      AddGameDataObject<BeaverTailPistachOhPlatedItem>();
      AddGameDataObject<BeaverTailStrawberryCheesecakePlatedItem>();
      AddGameDataObject<BeaverTailCocoVanilPlatedItem>();
      AddGameDataObject<BeaverTailBrwowniePlatedItem>();

      // base dish last, then the cards that require it
      AddGameDataObject<BeaverTailsDish>();
      AddGameDataObject<KillaloeSunriseCard>();
      AddGameDataObject<HazelAmourCard>();
      AddGameDataObject<BananaramaCard>();
      AddGameDataObject<ApplePieCard>();
      AddGameDataObject<TripleTripCard>();
      AddGameDataObject<AvalancheCard>();
      AddGameDataObject<MehpleCard>();
      AddGameDataObject<PistachOhCard>();
      AddGameDataObject<StrawberryCheesecakeCard>();
      AddGameDataObject<CocoVanilCard>();
      AddGameDataObject<BrwownieCard>();
      AddGameDataObject<FriesCard>();
      AddGameDataObject<PoutineCard>();
      AddGameDataObject<DoubleCheesePoutineCard>();
      AddGameDataObject<MapleSyrupCard>();

      Log( "registered: cinnamon, hazelnut jar + spread, 2 providers, "
          + "banana, caramelise process, caramel chain, fried chain, 7 recipe stages, 5 plated, dish, 4 recipe cards" );
    }
  }
}
