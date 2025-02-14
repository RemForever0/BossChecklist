﻿/*
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria.ModLoader;

namespace <YourModsNamespace>
{
	// This class provides an example of advanced Boss Checklist integration utilizing the "GetBossInfoDictionary" Mod.Call that other Mods can copy into their mod's source code.
	// If you are simply adding support for bosses in your mod to Boss Checklist, this is not what you want. Go read https://github.com/JavidPack/BossChecklist/wiki/Support-using-Mod-Call
	// By copying this class into your mod, you can access Boss Checklist boss data reliably and with type safety without requiring a strong dependency.
	public class BossChecklistIntegration : ModSystem
	{
		// Boss Checklist might add new features, so a version is passed into GetBossInfo. 
		// If a new version of the GetBossInfo Call is implemented, find this class in the Boss Checklist Github once again and replace this version with the new version: https://github.com/JavidPack/BossChecklist/blob/master/BossChecklistIntegrationExample.cs
		private static readonly Version BossChecklistAPIVersion = new Version(1, 6); // Do not change this yourself.

		public class BossChecklistBossInfo
		{
			internal string key = ""; // unique identifier for an entry
			internal string modSource = "";
			internal LocalizedText displayName = null;

			internal float progression = 0f; // See https://github.com/JavidPack/BossChecklist/blob/master/BossTracker.cs#L13 for vanilla boss values
			internal Func<bool> downed = () => false;

			internal bool isBoss = false;
			internal bool isMiniboss = false;
			internal bool isEvent = false;

			internal List<int> npcIDs = new List<int>(); // Does not include minions, only npcids that count towards the NPC still being alive.
			internal Func<LocalizedText> spawnInfo = null;
			internal List<int> spawnItems = new List<int>();
			internal int treasureBag = 0;
			internal List<int> dropRateInfo = new List<DropRateInfo>();
			internal List<int> loot = new List<int>();
			internal List<int> collectibles = new List<int>();
		}

		public static Dictionary<string, BossChecklistBossInfo> bossInfos = new Dictionary<string, BossChecklistBossInfo>();

		public static bool IntegrationSuccessful { get; private set; }

		public override void PostAddRecipes() {
			// For best results, this code is in PostAddRecipes
			bossInfos.Clear();

			if (ModLoader.TryGetMod("BossChecklist", out Mod bossChecklist) && bossChecklist.Version >= BossChecklistAPIVersion) {
				object currentBossInfoResponse = bossChecklist.Call("GetBossInfoDictionary", Mod, BossChecklistAPIVersion.ToString());
				if (currentBossInfoResponse is Dictionary<string, Dictionary<string, object>> bossInfoList) {
					bossInfos = bossInfoList.ToDictionary(boss => boss.Key, boss => new BossChecklistBossInfo() {
						key = boss.Value.ContainsKey("key") ? boss.Value["key"] as string : "",
						modSource = boss.Value.ContainsKey("modSource") ? boss.Value["modSource"] as string : "",
						displayName = boss.Value.ContainsKey("displayName") ? boss.Value["displayName"] as LocalizedText : null,

						progression = boss.Value.ContainsKey("progression") ? Convert.ToSingle(boss.Value["progression"]) : 0f,
						downed = boss.Value.ContainsKey("downed") ? boss.Value["downed"] as Func<bool> : () => false,

						isBoss = boss.Value.ContainsKey("isBoss") ? Convert.ToBoolean(boss.Value["isBoss"]) : false,
						isMiniboss = boss.Value.ContainsKey("isMiniboss") ? Convert.ToBoolean(boss.Value["isMiniboss"]) : false,
						isEvent = boss.Value.ContainsKey("isEvent") ? Convert.ToBoolean(boss.Value["isEvent"]) : false,

						npcIDs = boss.Value.ContainsKey("npcIDs") ? boss.Value["npcIDs"] as List<int> : new List<int>(),
						spawnInfo = boss.Value.ContainsKey("spawnInfo") ? boss.Value["spawnInfo"] as Func<LocalizedText> : null,
						spawnItems = boss.Value.ContainsKey("spawnItems") ? boss.Value["spawnItems"] as List<int> : new List<int>(),
						treasureBag = boss.Value.ContainsKey("treasureBag") ? boss.Value["treasureBag"] as int : 0,
						dropRateInfo = boss.Value.ContainsKey("dropRateInfo") ? boss.Value["dropRateInfo"] as List<DropRateInfo> : new List<DropRateInfo>(),
						loot = boss.Value.ContainsKey("loot") ? boss.Value["loot"] as List<int> : new List<int>(),
						collectibles = boss.Value.ContainsKey("collectibles") ? boss.Value["collectibles"] as List<int> : new List<int>(),
					});

					IntegrationSuccessful = true;
				}
			}
		}

		public override void Unload() {
			bossInfos.Clear();
		}

		// This method shows an example of using the BossChecklistBossInfo data for something cool in your mod.
		public static float DownedBossProgress() {
			if (bossInfos.Count == 0) // bossInfos might be empty, if BossChecklist isn't present or something goes wrong.
				return 0;

			return (float)bossInfos.Count(x => x.Value.downed()) / bossInfos.Count();
		}

		// This utility method shows how you can easily check downed bosses from mods without worrying about the typical cross mod headaches like reflection, strong/weak references, and obtaining dll files to reference.
		public static bool BossDowned(string bossKey) => bossInfos.TryGetValue(bossKey, out var bossInfo) ? bossInfo.downed() : false;
	}
}
*/
