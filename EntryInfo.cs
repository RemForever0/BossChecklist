﻿using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.Localization;
using Terraria.ID;
using Terraria.GameContent.ItemDropRules;
using Terraria.GameContent;
using ReLogic.Content;
using Microsoft.Xna.Framework.Graphics;
using System.Linq;
using Microsoft.Xna.Framework;

namespace BossChecklist
{
	internal class EntryInfo // Inheritance for Event instead?
	{
		// This localization-ignoring string is used for cross mod queries and networking. Each key is completely unique.
		internal string Key { get; init; }

		internal EntryType type;
		internal string modSource;
		internal LocalizedText name; // This should not be used for displaying purposes. Use 'EntryInfo.GetDisplayName' instead.
		internal List<int> npcIDs;
		internal float progression;
		internal Func<bool> downed;
		internal Func<bool> available;
		internal bool hidden;
		internal Func<NPC, LocalizedText> customDespawnMessages;

		internal List<string> relatedEntries;

		internal List<int> spawnItem;
		internal LocalizedText spawnInfo;

		internal int treasureBag = 0;
		internal List<int> collection;
		internal Dictionary<int, CollectionType> collectType;
		internal List<DropRateInfo> loot;
		internal List<int> lootItemTypes;

		internal Asset<Texture2D> portraitTexture; // used for vanilla entry portrait drawing
		internal Action<SpriteBatch, Rectangle, Color> customDrawing; // used for modded entry portrait drawing
		internal List<Asset<Texture2D>> headIconTextures;

		/*
		internal ExpandoObject ConvertToExpandoObject() {
			dynamic expando = new ExpandoObject();

			expando.key = Key;
			expando.modSource = modSource;
			expando.internalName = internalName;
			expando.displayName = name;

			expando.progression = progression;
			expando.downed = new Func<bool>(downed);

			expando.isBoss = type.Equals(EntryType.Boss);
			expando.isMiniboss = type.Equals(EntryType.MiniBoss);
			expando.isEvent = type.Equals(EntryType.Event);

			expando.npcIDs = new List<int>(npcIDs);
			expando.spawnItem = new List<int>(spawnItem);
			expando.loot = new List<int>(loot);
			expando.collection = new List<int>(collection);

			return expando;
		}
		*/

		internal Dictionary<string, object> ConvertToDictionary(Version GetEntryInfoAPIVersion) {
			// We may want to allow different returns based on api version.
			//if (GetEntryInfoAPIVersion == new Version(1, 1)) {
			var dict = new Dictionary<string, object> {
				{ "key", Key },
				{ "modSource", modSource },
				{ "displayName", name },

				{ "progression", progression },
				{ "downed", new Func<bool>(downed) },

				{ "isBoss", type.Equals(EntryType.Boss) },
				{ "isMiniboss", type.Equals(EntryType.MiniBoss) },
				{ "isEvent", type.Equals(EntryType.Event) },

				{ "npcIDs", new List<int>(npcIDs) },
				{ "spawnItem", new List<int>(spawnItem) },
				{ "treasureBag", treasureBag },
				{ "loot", new List<DropRateInfo>(loot) },
				{ "collection", new List<int>(collection) }
			};

			return dict;
		}

		internal string DisplayName => Language.GetTextValue(name.Key);

		internal string DisplaySpawnInfo => Language.GetTextValue(spawnInfo.Key);
		
		internal string SourceDisplayName => modSource == "Terraria" || modSource == "Unknown" ? modSource : SourceDisplayNameWithoutChatTags(ModLoader.GetMod(modSource).DisplayName);

		internal bool MarkedAsDowned => WorldAssist.MarkedEntries.Contains(this.Key);

		internal bool IsDownedOrMarked => downed() || MarkedAsDowned;

		internal int GetIndex => BossChecklist.bossTracker.SortedEntries.IndexOf(this);

		internal int GetRecordIndex => BossChecklist.bossTracker.BossRecordKeys.IndexOf(this.Key);

		internal static string SourceDisplayNameWithoutChatTags(string modSource) {
			string editedName = "";

			for (int c = 0; c < modSource.Length; c++) {
				// Add each character one by one to find chattags in order
				// Chat tags cannot be contained inside other chat tags so no need to worry about overlap
				editedName += modSource[c];
				if (editedName.Contains("[i:") && editedName.EndsWith("]")) {
					// Update return name if a complete item chat tag is found
					editedName = editedName.Substring(0, editedName.IndexOf("[i:"));
					continue;
				}
				if (editedName.Contains("[i/") && editedName.EndsWith("]")) {
					// Update return name if a complete item chat tag is found
					editedName = editedName.Substring(0, editedName.IndexOf("[i/"));
					continue;
				}
				if (editedName.Contains("[c/") && editedName.Contains(":") && editedName.EndsWith("]")) {
					// Color chat tags are edited differently as we want to keep the text that's nested inside them
					string part1 = editedName.Substring(0, editedName.IndexOf("[c/"));
					string part2 = editedName.Substring(editedName.IndexOf(":") + 1);
					part2 = part2.Substring(0, part2.Length - 1);
					editedName = part1 + part2;
					continue;
				}
			}
			return editedName;
		}

		/// <summary>
		/// Determines whether or not the entry should be visible on the Table of Contents, 
		/// based on configurations and filter status.
		/// </summary>
		/// <returns>If the entry should be visible</returns>
		internal bool VisibleOnChecklist() {
			bool HideUnsupported = modSource == "Unknown" && BossChecklist.BossLogConfig.HideUnsupported; // entries not using the new mod calls for the Boss Log
			bool HideUnavailable = !available() && BossChecklist.BossLogConfig.HideUnavailable && !BossUISystem.Instance.BossLog.showHidden && !IsDownedOrMarked; // entries that are labeled as not available
			bool HideHidden = hidden && !BossUISystem.Instance.BossLog.showHidden; // entries that are labeled as hidden
			bool SkipNonBosses = BossChecklist.BossLogConfig.OnlyShowBossContent && type != EntryType.Boss; // if the user has the config to only show bosses and the entry is not a boss
			if (HideUnavailable || HideHidden || SkipNonBosses || HideUnsupported) {
				return false;
			}

			// Make sure the filters allow the entry to be visible
			string bFilter = BossChecklist.BossLogConfig.FilterBosses;
			string mbFilter = BossChecklist.BossLogConfig.FilterMiniBosses;
			string eFilter = BossChecklist.BossLogConfig.FilterEvents;

			bool FilterBoss = type == EntryType.Boss && bFilter == "Hide when completed" && IsDownedOrMarked;
			bool FilterMiniBoss = type == EntryType.MiniBoss && (mbFilter == "Hide" || (mbFilter == "Hide when completed" && IsDownedOrMarked));
			bool FilterEvent = type == EntryType.Event && (eFilter == "Hide" || (eFilter == "Hide when completed" && IsDownedOrMarked));
			if (FilterBoss || FilterMiniBoss || FilterEvent) {
				return false;
			}

			return true; // if it passes all the checks, it should be shown
		}

		internal EntryInfo(EntryType entryType, string modSource, string internalName, float progression, LocalizedText name, List<int> npcIDs, Func<bool> downed, LocalizedText spawnInfo, Dictionary<string, object> extraData = null) {
			// Add the mod source to the opted mods list of the credits page if its not already and add the entry type
			if (modSource != "Terraria" && modSource != "Unknown") {
				BossUISystem.Instance.RegisteredMods.TryAdd(modSource, new int[3]);
				BossUISystem.Instance.RegisteredMods[modSource][(int)entryType]++;
			}

			// required entry data
			this.Key = modSource + " " + internalName;
			this.type = entryType;
			this.modSource = modSource;
			this.progression = progression;

			this.name = name;
			this.npcIDs = npcIDs ?? new List<int>();
			this.downed = downed;
			this.spawnInfo = spawnInfo;

			// self-initializing data
			this.hidden = false; // defaults to false, hidden status can be toggled per world
			this.relatedEntries = new List<string>(); /// Setup in <see cref="BossTracker.SetupEntryRelations"/>
			this.loot = new List<DropRateInfo>(); /// Setup in <see cref="BossTracker.FinalizeEntryLootTables"/>
			this.lootItemTypes = new List<int>(); /// Setup in <see cref="BossTracker.FinalizeEntryLootTables"/>
			this.collectType = new Dictionary<int, CollectionType>(); /// Setup in <see cref="BossTracker.FinalizeCollectionTypes"/>

			// optional extra data
			List<int> InterpretObjectAsListOfInt(object data) => data is List<int> ? data as List<int> : (data is int ? new List<int>() { Convert.ToInt32(data) } : new List<int>());
			List<string> InterpretObjectAsListOfStrings(object data) => data is List<string> ? data as List<string> : (data is string ? new List<string>() { data as string } : null);

			this.available = extraData?.ContainsKey("availability") == true ? extraData["availability"] as Func<bool> : () => true;
			this.spawnItem = extraData?.ContainsKey("spawnItems") == true ? InterpretObjectAsListOfInt(extraData["spawnItems"]) : new List<int>();
			this.collection = extraData?.ContainsKey("collectibles") == true ? InterpretObjectAsListOfInt(extraData["collectibles"]) : new List<int>();
			this.customDrawing = extraData?.ContainsKey("customPortrait") == true ? extraData["customPortrait"] as Action<SpriteBatch, Rectangle, Color> : null;
			this.customDespawnMessages = entryType != EntryType.Event && extraData?.ContainsKey("despawnMessage") == true ? extraData["despawnMessage"] as Func<NPC, LocalizedText> : null;

			headIconTextures = new List<Asset<Texture2D>>();
			if (extraData?.ContainsKey("overrideHeadTextures") == true) {
				foreach (string texturePath in InterpretObjectAsListOfStrings(extraData["overrideHeadTextures"])) {
					headIconTextures.Add(ModContent.Request<Texture2D>(texturePath, AssetRequestMode.ImmediateLoad));
				}
			}
			else {
				foreach (int npc in npcIDs) {
					if (entryType != EntryType.Event && NPCID.Sets.BossHeadTextures[npc] != -1)
						headIconTextures.Add(TextureAssets.NpcHeadBoss[NPCID.Sets.BossHeadTextures[npc]]); // Skip events. Events must use a custom icon to display.
				}
			}

			if (headIconTextures.Count == 0)
				headIconTextures.Add(TextureAssets.NpcHead[0]); // If the head textures is empty, fill it with the '?' head icon so modder's see something is wrong
		}

		// Workaround for vanilla events with illogical translation keys.
		internal EntryInfo WithCustomTranslationKey(string translationKey) {
			// EntryInfo.name should remain as a translation key.
			this.name = Language.GetText(translationKey);
			return this;
		}

		internal EntryInfo WithCustomAvailability(Func<bool> funcBool) {
			this.available = funcBool;
			return this;
		}

		internal EntryInfo WithCustomPortrait(string texturePath) {
			if (ModContent.HasAsset(texturePath)) {
				this.portraitTexture = ModContent.Request<Texture2D>(texturePath);
			}
			return this;
		}

		internal EntryInfo WithCustomHeadIcon(string texturePath) {
			if (ModContent.HasAsset(texturePath)) {
				this.headIconTextures = new List<Asset<Texture2D>>() { ModContent.Request<Texture2D>(texturePath) };
			}
			else {
				this.headIconTextures = new List<Asset<Texture2D>>() { TextureAssets.NpcHead[0] };
			}
			return this;
		}

		internal EntryInfo WithCustomHeadIcon(List<string> texturePaths) {
			this.headIconTextures = new List<Asset<Texture2D>>();
			foreach (string path in texturePaths) {
				if (ModContent.HasAsset(path)) {
					this.headIconTextures.Add(ModContent.Request<Texture2D>(path));
				}
			}
			if (headIconTextures.Count == 0) {
				this.headIconTextures = new List<Asset<Texture2D>>() { TextureAssets.NpcHead[0] };
			}
			return this;
		}
		internal static EntryInfo MakeVanillaBoss(EntryType type, float val, string key, int npcID, Func<bool> downed) {
			string nameKey = key.Substring(key.LastIndexOf(".") + 1);
			string tremor = nameKey == "MoodLord" && BossChecklist.tremorLoaded ? "_Tremor" : "";

			Func<NPC, LocalizedText> customMessages = null;
			if (type == EntryType.Boss) { // BossChecklist only has despawn messages for vanilla Bosses
				List<int> DayDespawners = new List<int>() {
					NPCID.EyeofCthulhu,
					NPCID.Retinazer,
					NPCID.Spazmatism,
					NPCID.TheDestroyer,
				};

				customMessages = delegate (NPC npc) {
					if (Main.player.All(plr => !plr.active || plr.dead)) {
						return Language.GetText($"{NPCAssist.LangChat}.Loss.{nameKey}"); // Despawn message when all players are dead
					}
					else if (Main.dayTime && DayDespawners.Contains(npc.type)) {
						return Language.GetText($"{NPCAssist.LangChat}.Despawn.Day"); // Despawn message when it turns to day
					}

					// unique despawn messages should default to the generic message when no conditions are met
					return Language.GetText($"{NPCAssist.LangChat}.Despawn.Generic");
				};
			}

			return new EntryInfo(
				entryType: type,
				modSource: "Terraria",
				internalName: nameKey,
				progression: val,
				name: Language.GetText(key),
				npcIDs: new List<int>() { npcID },
				downed: downed,
				Language.GetText($"Mods.BossChecklist.BossSpawnInfo.{nameKey}{tremor}"),
				extraData: new Dictionary<string, object>() {
					{ "spawnItem", BossChecklist.bossTracker.EntrySpawnItems.GetValueOrDefault($"Terraria {nameKey}") },
					{ "collectibles", BossChecklist.bossTracker.EntryCollections.GetValueOrDefault($"Terraria {nameKey}") },
					{ "despawnMessage", customMessages },
				}
			);
		}

		internal static EntryInfo MakeVanillaBoss(EntryType type, float val, string key, List<int> ids, Func<bool> downed) {
			string nameKey = key.Substring(key.LastIndexOf(".") + 1).Replace(" ", "").Replace("'", "");
			string tremor = nameKey == "MoodLord" && BossChecklist.tremorLoaded ? "_Tremor" : "";

			Func<NPC, LocalizedText> customMessages = null;
			if (type == EntryType.Boss) {
				List<int> DayDespawners = new List<int>() {
					NPCID.EyeofCthulhu,
					NPCID.Retinazer,
					NPCID.Spazmatism,
					NPCID.TheDestroyer,
				};

				bool DayCheck(int type) => Main.dayTime && DayDespawners.Contains(type);
				bool AllPlayersAreDead() => Main.player.All(plr => !plr.active || plr.dead);
				string customKey = $"{NPCAssist.LangChat}.Loss.{nameKey}";
				customMessages = npc => Language.GetText(AllPlayersAreDead() ? customKey : DayCheck(npc.type) ? $"{NPCAssist.LangChat}.Despawn.Day" : $"{NPCAssist.LangChat}.Despawn.Generic");
			}

			return new EntryInfo(
				entryType: type,
				modSource: "Terraria",
				internalName: nameKey,
				progression: val,
				name: Language.GetText(key),
				npcIDs: ids,
				downed: downed,
				spawnInfo: Language.GetText($"Mods.BossChecklist.BossSpawnInfo.{nameKey}{tremor}"),
				extraData: new Dictionary<string, object>() {
					{ "spawnItem", BossChecklist.bossTracker.EntrySpawnItems.GetValueOrDefault($"Terraria {nameKey}") },
					{ "collectibles", BossChecklist.bossTracker.EntryCollections.GetValueOrDefault($"Terraria {nameKey}") },
					{ "despawnMessage", customMessages },
				}
			);
		}

		internal static EntryInfo MakeVanillaEvent(float val, string key, Func<bool> downed) {
			string nameKey = key.Substring(key.LastIndexOf(".") + 1).Replace(" ", "").Replace("'", "");
			return new EntryInfo(
				entryType: EntryType.Event,
				modSource: "Terraria",
				internalName: nameKey,
				progression: val,
				name: Language.GetText(key),
				npcIDs: BossChecklist.bossTracker.EventNPCs.GetValueOrDefault($"Terraria {nameKey}"),
				downed: downed,
				spawnInfo: Language.GetText($"Mods.BossChecklist.BossSpawnInfo.{nameKey}"),
				extraData: new Dictionary<string, object>() {
					{ "spawnItem", BossChecklist.bossTracker.EntrySpawnItems.GetValueOrDefault($"Terraria {nameKey}") },
					{ "collectibles", BossChecklist.bossTracker.EntryCollections.GetValueOrDefault($"Terraria {nameKey}") },
				}
			);
		}

		public override string ToString() => $"{progression} {Key}";
	}

	internal class OrphanInfo
	{
		internal OrphanType type;
		internal string Key;
		internal string modSource;
		internal string bossName;

		internal List<int> values;
		// Use cases for values...
		/// Adding Spawn Item IDs to a boss
		/// Adding Collectible item IDs to a boss
		/// Adding NPC IDs to an event

		internal OrphanInfo(OrphanType type, string bossKey, List<int> values) {
			this.type = type;
			this.Key = bossKey;
			this.values = values;

			List<EntryInfo> bosses = BossChecklist.bossTracker.SortedEntries;
			int index = bosses.FindIndex(x => x.Key == this.Key);
			if (index != -1) {
				modSource = bosses[index].SourceDisplayName;
				bossName = bosses[index].DisplayName;
			}
			else {
				modSource = "Unknown";
				bossName = "Unknown";
			}
		}
	}
}
